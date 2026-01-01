using LenkCareHomes.Api.Domain.Entities;

namespace LenkCareHomes.Api.Services.Audit;

/// <summary>
///     Service interface for audit log operations in Cosmos DB.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    ///     Gets whether the audit log service is configured and available.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    ///     Logs an audit entry asynchronously without blocking the request.
    /// </summary>
    /// <param name="entry">The audit log entry to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Logs an authentication event.
    /// </summary>
    Task LogAuthenticationEventAsync(
        string action,
        string outcome,
        Guid? userId,
        string? userEmail,
        string? ipAddress,
        string? userAgent,
        string? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Logs a PHI access event.
    /// </summary>
    Task LogPhiAccessAsync(
        string action,
        Guid userId,
        string userEmail,
        string resourceType,
        string resourceId,
        string outcome,
        string? ipAddress,
        string? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Queries audit logs with filtering and pagination.
    /// </summary>
    /// <param name="filter">Query filter parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated audit log entries.</returns>
    Task<AuditLogQueryResult> QueryLogsAsync(AuditLogQueryFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets audit log statistics for the specified time period.
    /// </summary>
    /// <param name="since">Start of the time period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Action counts for the period.</returns>
    Task<Dictionary<string, int>> GetStatsAsync(DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clears all audit logs from the container.
    ///     WARNING: This is destructive and only for development use.
    /// </summary>
    /// <returns>The number of audit logs deleted.</returns>
    Task<int> ClearAllLogsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Filter parameters for querying audit logs.
/// </summary>
public sealed record AuditLogQueryFilter
{
    public Guid? UserId { get; init; }
    public string? Action { get; init; }
    public string? ResourceType { get; init; }
    public string? ResourceId { get; init; }
    public string? Outcome { get; init; }
    public string? SearchText { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageSize { get; init; } = 50;
    public string? ContinuationToken { get; init; }
}

/// <summary>
///     Result of an audit log query.
/// </summary>
public sealed record AuditLogQueryResult
{
    public required IReadOnlyList<AuditLogEntry> Entries { get; init; }
    public string? ContinuationToken { get; init; }
}