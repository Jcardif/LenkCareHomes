using LenkCareHomes.Api.Domain.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace LenkCareHomes.Api.Services.Audit;

/// <summary>
///     Cosmos DB implementation of the audit log service.
///     Writes audit entries asynchronously to maintain performance.
/// </summary>
public sealed class CosmosAuditLogService : IAuditLogService, IDisposable
{
    private readonly Container? _container;
    private readonly CosmosClient? _cosmosClient;
    private readonly ILogger<CosmosAuditLogService> _logger;
    private readonly CosmosDbSettings _settings;

    public CosmosAuditLogService(
        IOptions<CosmosDbSettings> settings,
        ILogger<CosmosAuditLogService> logger,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(settings?.Value);
        _logger = logger;
        _settings = settings.Value;

        var connectionString = settings.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("Cosmos DB connection string not configured. Audit logging will be disabled.");
            _cosmosClient = null;
            _container = null;
            return;
        }

        var clientOptions = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        // In development, disable SSL validation for Cosmos DB Emulator
        // The emulator uses a self-signed certificate
        if (environment.IsDevelopment() &&
            connectionString.Contains("localhost:8081", StringComparison.OrdinalIgnoreCase))
        {
            clientOptions.HttpClientFactory = () =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                return new HttpClient(handler);
            };
            clientOptions.ConnectionMode = ConnectionMode.Gateway;
            _logger.LogInformation("Cosmos DB configured for local emulator with SSL validation disabled");
        }

        _cosmosClient = new CosmosClient(connectionString, clientOptions);
        _container = _cosmosClient.GetContainer(settings.Value.DatabaseName, settings.Value.ContainerName);
    }

    /// <inheritdoc />
    public bool IsConfigured => _container is not null;

    /// <inheritdoc />
    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        if (_container is null)
        {
            _logger.LogDebug("Audit logging skipped - Cosmos DB not configured");
            return;
        }

        try
        {
            // Fire and forget pattern for audit logging to avoid blocking requests
            await _container.CreateItemAsync(entry, new PartitionKey(entry.PartitionKey),
                cancellationToken: cancellationToken);
            _logger.LogDebug("Audit log entry created: {Action} by {UserId}", entry.Action, entry.UserId);
        }
        catch (CosmosException ex)
        {
            // Log the error but don't fail the request
            _logger.LogError(ex, "Failed to write audit log entry: {Action}", entry.Action);
        }
    }

    /// <inheritdoc />
    public Task LogAuthenticationEventAsync(
        string action,
        string outcome,
        Guid? userId,
        string? userEmail,
        string? ipAddress,
        string? userAgent,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            PartitionKey = userId?.ToString() ?? "anonymous",
            Action = action,
            Outcome = outcome,
            UserId = userId,
            UserEmail = userEmail,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = details,
            ResourceType = "Authentication"
        };

        return LogAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public Task LogPhiAccessAsync(
        string action,
        Guid userId,
        string userEmail,
        string resourceType,
        string resourceId,
        string outcome,
        string? ipAddress,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            PartitionKey = userId.ToString(),
            Action = action,
            Outcome = outcome,
            UserId = userId,
            UserEmail = userEmail,
            ResourceType = resourceType,
            ResourceId = resourceId,
            IpAddress = ipAddress,
            Details = details
        };

        return LogAsync(entry, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ClearAllLogsAsync(CancellationToken cancellationToken = default)
    {
        if (_container is null)
        {
            _logger.LogDebug("Audit log clear skipped - Cosmos DB not configured");
            return 0;
        }

        var deletedCount = 0;

        try
        {
            // Query all documents
            var query = new QueryDefinition("SELECT c.id, c.partitionKey FROM c");
            var iterator = _container.GetItemQueryIterator<dynamic>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                foreach (var item in response)
                {
                    string id = item.id;
                    string partitionKey = item.partitionKey;

                    await _container.DeleteItemAsync<dynamic>(
                        id,
                        new PartitionKey(partitionKey),
                        cancellationToken: cancellationToken);
                    deletedCount++;
                }
            }

            _logger.LogWarning("Cleared {Count} audit log entries from Cosmos DB", deletedCount);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to clear audit logs from Cosmos DB");
            throw;
        }

        return deletedCount;
    }

    /// <inheritdoc />
    public async Task<AuditLogQueryResult> QueryLogsAsync(AuditLogQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        if (_container is null) return new AuditLogQueryResult { Entries = [] };

        var pageSize = Math.Min(Math.Max(filter.PageSize, 1), 100);
        var (queryText, parameters) = BuildAuditQuery(filter);

        var queryDefinition = new QueryDefinition(queryText);
        foreach (var param in parameters) queryDefinition = queryDefinition.WithParameter(param.Key, param.Value);

        var requestOptions = new QueryRequestOptions { MaxItemCount = pageSize };
        var iterator = _container.GetItemQueryIterator<AuditLogEntry>(
            queryDefinition,
            filter.ContinuationToken,
            requestOptions);

        var entries = new List<AuditLogEntry>();
        string? nextContinuationToken = null;

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            entries.AddRange(response);
            nextContinuationToken = response.ContinuationToken;
        }

        return new AuditLogQueryResult
        {
            Entries = entries,
            ContinuationToken = nextContinuationToken
        };
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetStatsAsync(DateTime since,
        CancellationToken cancellationToken = default)
    {
        if (_container is null) return new Dictionary<string, int>();

        var queryText = @"
            SELECT c.action, COUNT(1) as count 
            FROM c 
            WHERE c.timestamp >= @since 
            GROUP BY c.action";

        var queryDefinition = new QueryDefinition(queryText)
            .WithParameter("@since", since.ToString("o"));

        var iterator = _container.GetItemQueryIterator<ActionCountResult>(queryDefinition);
        var actionCounts = new Dictionary<string, int>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in response) actionCounts[item.Action] = item.Count;
        }

        return actionCounts;
    }

    public void Dispose()
    {
        _cosmosClient?.Dispose();
    }

    /// <summary>
    ///     Ensures the database and container exist in Cosmos DB.
    ///     Called during application startup in development.
    /// </summary>
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        if (_cosmosClient is null)
        {
            _logger.LogDebug("Cosmos DB not configured, skipping database creation");
            return;
        }

        try
        {
            var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                _settings.DatabaseName,
                cancellationToken: cancellationToken);

            await database.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(_settings.ContainerName, "/partitionKey"),
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Cosmos DB database '{Database}' and container '{Container}' ensured",
                _settings.DatabaseName,
                _settings.ContainerName);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to ensure Cosmos DB database/container exists");
            throw;
        }
    }

    /// <summary>
    ///     Builds the Cosmos DB query for audit logs with all filter conditions.
    /// </summary>
    private static (string QueryText, Dictionary<string, object> Parameters) BuildAuditQuery(AuditLogQueryFilter filter)
    {
        var queryParts = new List<string> { "SELECT * FROM c" };
        var conditions = new List<string>();
        var parameters = new Dictionary<string, object>();

        if (filter.UserId.HasValue)
        {
            conditions.Add("c.userId = @userId");
            parameters["@userId"] = filter.UserId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            conditions.Add("c.action = @action");
            parameters["@action"] = filter.Action;
        }

        if (!string.IsNullOrWhiteSpace(filter.ResourceType))
        {
            conditions.Add("CONTAINS(LOWER(c.resourceType), LOWER(@resourceType))");
            parameters["@resourceType"] = filter.ResourceType;
        }

        if (!string.IsNullOrWhiteSpace(filter.ResourceId))
        {
            conditions.Add("c.resourceId = @resourceId");
            parameters["@resourceId"] = filter.ResourceId;
        }

        if (!string.IsNullOrWhiteSpace(filter.Outcome))
        {
            conditions.Add("c.outcome = @outcome");
            parameters["@outcome"] = filter.Outcome;
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            conditions.Add("(CONTAINS(LOWER(c.userEmail), LOWER(@searchText)) OR " +
                           "CONTAINS(LOWER(c.resourceType), LOWER(@searchText)) OR " +
                           "CONTAINS(LOWER(c.resourceId), LOWER(@searchText)) OR " +
                           "CONTAINS(LOWER(c.requestPath), LOWER(@searchText)) OR " +
                           "CONTAINS(LOWER(c.details), LOWER(@searchText)))");
            parameters["@searchText"] = filter.SearchText;
        }

        if (filter.FromDate.HasValue)
        {
            conditions.Add("c.timestamp >= @fromDate");
            parameters["@fromDate"] = filter.FromDate.Value.ToString("o");
        }

        if (filter.ToDate.HasValue)
        {
            conditions.Add("c.timestamp <= @toDate");
            parameters["@toDate"] = filter.ToDate.Value.ToString("o");
        }

        if (conditions.Count > 0) queryParts.Add("WHERE " + string.Join(" AND ", conditions));

        queryParts.Add("ORDER BY c.timestamp DESC");

        return (string.Join(" ", queryParts), parameters);
    }

    private sealed class ActionCountResult
    {
        public string Action { get; } = string.Empty;
        public int Count { get; set; }
    }
}

/// <summary>
///     Configuration settings for Cosmos DB audit logging.
/// </summary>
public sealed class CosmosDbSettings
{
    public const string SectionName = "CosmosDb";

    /// <summary>
    ///     Gets or sets the Cosmos DB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the database name.
    /// </summary>
    public string DatabaseName { get; set; } = "LenkCareHomes";

    /// <summary>
    ///     Gets or sets the container name for audit logs.
    /// </summary>
    public string ContainerName { get; set; } = "AuditLogs";
}