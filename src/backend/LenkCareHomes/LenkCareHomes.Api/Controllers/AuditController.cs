using System.Text;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
///     Controller for audit log access (Admin and Sysadmin only).
///     Provides comprehensive audit log viewing and filtering interface for HIPAA compliance.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IAuditLogService auditLogService,
        ILogger<AuditController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    ///     Gets recent audit log entries with advanced filtering capabilities.
    ///     Supports filtering by user, action, date range, resource, outcome, and free text search.
    /// </summary>
    /// <param name="userId">Optional user ID filter.</param>
    /// <param name="action">Optional action type filter.</param>
    /// <param name="resourceType">Optional resource type filter (e.g., "clients", "documents").</param>
    /// <param name="resourceId">Optional specific resource ID filter.</param>
    /// <param name="outcome">Optional outcome filter (Success, Failure, Denied).</param>
    /// <param name="searchText">Optional free text search across user email, resource, and details.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="pageSize">Number of results per page (default 50, max 100).</param>
    /// <param name="continuationToken">Continuation token for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log entries with pagination support.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AuditLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AuditLogResponse>> GetAuditLogsAsync(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] string? resourceId = null,
        [FromQuery] string? outcome = null,
        [FromQuery] string? searchText = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? continuationToken = null,
        CancellationToken cancellationToken = default)
    {
        if (!_auditLogService.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Audit log service is not configured." });

        var filter = new AuditLogQueryFilter
        {
            UserId = userId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Outcome = outcome,
            SearchText = searchText,
            FromDate = fromDate,
            ToDate = toDate,
            PageSize = pageSize,
            ContinuationToken = continuationToken
        };

        var result = await _auditLogService.QueryLogsAsync(filter, cancellationToken);

        return Ok(new AuditLogResponse
        {
            Entries = result.Entries,
            ContinuationToken = result.ContinuationToken
        });
    }

    /// <summary>
    ///     Exports audit logs to CSV format for compliance audits.
    ///     Supports the same filtering options as the main endpoint.
    /// </summary>
    /// <param name="userId">Optional user ID filter.</param>
    /// <param name="action">Optional action type filter.</param>
    /// <param name="resourceType">Optional resource type filter.</param>
    /// <param name="resourceId">Optional specific resource ID filter.</param>
    /// <param name="outcome">Optional outcome filter.</param>
    /// <param name="searchText">Optional free text search.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="maxRecords">Maximum records to export (default 10000, max 50000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CSV file with audit log entries.</returns>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ExportAuditLogsCsvAsync(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] string? resourceId = null,
        [FromQuery] string? outcome = null,
        [FromQuery] string? searchText = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int maxRecords = 10000,
        CancellationToken cancellationToken = default)
    {
        if (!_auditLogService.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Audit log service is not configured." });

        maxRecords = Math.Min(Math.Max(maxRecords, 1), 50000);

        var entries = new List<AuditLogEntry>();
        string? continuationToken = null;

        // Fetch all entries up to maxRecords using pagination
        while (entries.Count < maxRecords)
        {
            var filter = new AuditLogQueryFilter
            {
                UserId = userId,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Outcome = outcome,
                SearchText = searchText,
                FromDate = fromDate,
                ToDate = toDate,
                PageSize = Math.Min(100, maxRecords - entries.Count),
                ContinuationToken = continuationToken
            };

            var result = await _auditLogService.QueryLogsAsync(filter, cancellationToken);
            entries.AddRange(result.Entries);
            continuationToken = result.ContinuationToken;

            if (string.IsNullOrEmpty(continuationToken))
                break;
        }

        // Build CSV content
        var csv = new StringBuilder();
        csv.AppendLine(
            "Timestamp,User Email,User ID,Action,Outcome,Resource Type,Resource ID,HTTP Method,Request Path,Status Code,IP Address,Details");

        foreach (var entry in entries)
            csv.AppendLine(
                $"\"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}\"," +
                $"\"{EscapeCsvField(entry.UserEmail)}\"," +
                $"\"{entry.UserId}\"," +
                $"\"{EscapeCsvField(entry.Action)}\"," +
                $"\"{EscapeCsvField(entry.Outcome)}\"," +
                $"\"{EscapeCsvField(entry.ResourceType)}\"," +
                $"\"{EscapeCsvField(entry.ResourceId)}\"," +
                $"\"{EscapeCsvField(entry.HttpMethod)}\"," +
                $"\"{EscapeCsvField(entry.RequestPath)}\"," +
                $"\"{entry.StatusCode}\"," +
                $"\"{EscapeCsvField(entry.IpAddress)}\"," +
                $"\"{EscapeCsvField(entry.Details)}\"");

        var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());

        _logger.LogInformation("Audit log export generated with {Count} entries", entries.Count);

        return File(bytes, "text/csv", fileName);
    }

    /// <summary>
    ///     Gets available action types for filtering.
    /// </summary>
    /// <returns>List of distinct action types.</returns>
    [HttpGet("actions")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult GetAvailableActions()
    {
        // Return all known audit actions from the constants
        var actions = typeof(AuditActions)
            .GetFields()
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => f.GetValue(null)?.ToString())
            .Where(v => v is not null)
            .OrderBy(v => v)
            .ToList();

        return Ok(actions);
    }

    /// <summary>
    ///     Gets audit log statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audit log statistics.</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(AuditLogStats), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AuditLogStats>> GetAuditStatsAsync(CancellationToken cancellationToken)
    {
        if (!_auditLogService.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Audit log service is not configured." });

        var since = DateTime.UtcNow.AddHours(-24);
        var actionCounts = await _auditLogService.GetStatsAsync(since, cancellationToken);

        return Ok(new AuditLogStats
        {
            ActionCounts = actionCounts,
            Since = since
        });
    }

    /// <summary>
    ///     Escapes a field for CSV export.
    /// </summary>
    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Escape double quotes by doubling them
        return value.Replace("\"", "\"\"");
    }
}

/// <summary>
///     Response model for audit log queries.
/// </summary>
public sealed record AuditLogResponse
{
    public required IReadOnlyList<AuditLogEntry> Entries { get; init; }
    public string? ContinuationToken { get; init; }
}

/// <summary>
///     Audit log statistics.
/// </summary>
public sealed record AuditLogStats
{
    public required Dictionary<string, int> ActionCounts { get; init; }
    public required DateTime Since { get; init; }
}