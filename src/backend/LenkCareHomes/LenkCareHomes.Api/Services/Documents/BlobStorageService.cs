using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Azure.Storage;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace LenkCareHomes.Api.Services.Documents;

/// <summary>
/// Azure Blob Storage service for document storage operations.
/// Supports both Azure Blob Storage and Azurite emulator for local development.
/// </summary>
public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageSettings _settings;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly bool _isEmulator;
    private readonly StorageSharedKeyCredential? _sharedKeyCredential;
    private readonly ConcurrentDictionary<string, BlobContainerClient> _containerClients = new();

    public BlobStorageService(
        IOptions<BlobStorageSettings> settings,
        ILogger<BlobStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
        {
            throw new InvalidOperationException("BlobStorage:ConnectionString is not configured. Please add it to your configuration.");
        }

        // Detect if using Azurite emulator (devstoreaccount1 is the well-known emulator account)
        _isEmulator = _settings.ConnectionString.Contains("devstoreaccount1", StringComparison.OrdinalIgnoreCase);

        _blobServiceClient = new BlobServiceClient(_settings.ConnectionString);

        // Extract shared key credential for SAS generation with emulator
        if (_isEmulator)
        {
            // Azurite well-known account key
            _sharedKeyCredential = new StorageSharedKeyCredential(
                "devstoreaccount1",
                "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==");
            _logger.LogInformation("Blob storage configured for Azurite emulator");
        }
    }

    /// <summary>
    /// Gets or creates a container client for the specified container name.
    /// Creates the container if it doesn't exist.
    /// </summary>
    private async Task<BlobContainerClient> GetContainerClientAsync(string containerName)
    {
        if (_containerClients.TryGetValue(containerName, out var existingClient))
        {
            return existingClient;
        }

        var client = _blobServiceClient.GetBlobContainerClient(containerName);
        
        // Create container if it doesn't exist
        await client.CreateIfNotExistsAsync();
        
        _containerClients.TryAdd(containerName, client);
        _logger.LogDebug("Created container client for {ContainerName}", containerName);
        return client;
    }

    /// <inheritdoc />
    public async Task<(string SasUrl, DateTime ExpiresAt)> GetUploadSasUrlAsync(
        string blobPath,
        string contentType,
        int expirationMinutes = 5,
        string containerName = BlobContainers.Documents)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = expiresAt,
            // Use HTTP for emulator, HTTPS for production
            Protocol = _isEmulator ? SasProtocol.HttpsAndHttp : SasProtocol.Https
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        // Generate SAS token - use shared key credential for emulator
        Uri sasUri;
        if (_isEmulator && _sharedKeyCredential is not null)
        {
            var sasToken = sasBuilder.ToSasQueryParameters(_sharedKeyCredential);
            sasUri = new Uri($"{blobClient.Uri}?{sasToken}");
        }
        else
        {
            sasUri = blobClient.GenerateSasUri(sasBuilder);
        }

        _logger.LogInformation(
            "Generated upload SAS for {Container}/{BlobPath}, expires at {ExpiresAt}",
            containerName,
            blobPath,
            expiresAt);

        return (sasUri.ToString(), expiresAt);
    }

    /// <inheritdoc />
    public async Task<(string SasUrl, DateTime ExpiresAt)> GetReadSasUrlAsync(
        string blobPath,
        int expirationMinutes = 5,
        string containerName = BlobContainers.Documents)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = expiresAt,
            // Use HTTP for emulator, HTTPS for production
            Protocol = _isEmulator ? SasProtocol.HttpsAndHttp : SasProtocol.Https
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        // Generate SAS token - use shared key credential for emulator
        Uri sasUri;
        if (_isEmulator && _sharedKeyCredential is not null)
        {
            var sasToken = sasBuilder.ToSasQueryParameters(_sharedKeyCredential);
            sasUri = new Uri($"{blobClient.Uri}?{sasToken}");
        }
        else
        {
            sasUri = blobClient.GenerateSasUri(sasBuilder);
        }

        _logger.LogInformation(
            "Generated read SAS for {Container}/{BlobPath}, expires at {ExpiresAt}",
            containerName,
            blobPath,
            expiresAt);

        return (sasUri.ToString(), expiresAt);
    }

    /// <inheritdoc />
    public async Task DeleteBlobAsync(string blobPath, string containerName = BlobContainers.Documents)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        await blobClient.DeleteIfExistsAsync();

        _logger.LogInformation("Deleted blob {Container}/{BlobPath}", containerName, blobPath);
    }

    /// <inheritdoc />
    public async Task UploadBlobAsync(string blobPath, byte[] content, string contentType, string containerName = BlobContainers.Documents)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        
        using var stream = new MemoryStream(content);
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
        
        _logger.LogInformation("Uploaded blob {Container}/{BlobPath} ({Size} bytes)", containerName, blobPath, content.Length);
    }

    /// <inheritdoc />
    public async Task UploadBlobAsync(string blobPath, Stream content, string contentType, string containerName = BlobContainers.Documents)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        
        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });
        
        _logger.LogInformation("Uploaded blob {Container}/{BlobPath}", containerName, blobPath);
    }

    /// <inheritdoc />
    public async Task<bool> BlobExistsAsync(string blobPath, string containerName = BlobContainers.Documents)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        return await blobClient.ExistsAsync();
    }

    /// <inheritdoc />
    public async Task<byte[]> DownloadBlobAsync(string blobPath, string containerName = BlobContainers.Documents, CancellationToken cancellationToken = default)
    {
        var containerClient = await GetContainerClientAsync(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);
        
        using var stream = new MemoryStream();
        await blobClient.DownloadToAsync(stream, cancellationToken);
        
        _logger.LogInformation("Downloaded blob {Container}/{BlobPath} ({Size} bytes)", containerName, blobPath, stream.Length);
        return stream.ToArray();
    }

    /// <inheritdoc />
    public async Task<int> DeleteAllBlobsAsync(string? containerName = null)
    {
        var deletedCount = 0;

        // If no container specified, delete from all known containers
        var containersToDelete = containerName is not null
            ? [containerName]
            : new[] { BlobContainers.Documents, BlobContainers.IncidentPhotos };

        foreach (var container in containersToDelete)
        {
            var containerClient = await GetContainerClientAsync(container);
            
            try
            {
                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    await blobClient.DeleteIfExistsAsync();
                    deletedCount++;
                }
                _logger.LogWarning("Deleted blobs from container {Container}", container);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogDebug("Container {Container} does not exist, skipping", container);
            }
        }

        _logger.LogWarning("Deleted {Count} blobs total from {ContainerCount} containers", deletedCount, containersToDelete.Length);
        return deletedCount;
    }
}

/// <summary>
/// Configuration settings for Azure Blob Storage.
/// </summary>
public sealed class BlobStorageSettings
{
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Gets or sets the Azure Blob Storage connection string.
    /// This can be set via BlobStorage:ConnectionString or ConnectionStrings:blobs (Aspire).
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container name for documents.
    /// </summary>
    public string ContainerName { get; set; } = "documents";

    /// <summary>
    /// Gets or sets the maximum file size in bytes (default 50MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;
}
