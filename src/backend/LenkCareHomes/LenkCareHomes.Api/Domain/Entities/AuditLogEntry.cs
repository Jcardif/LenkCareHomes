using System.Text.Json.Serialization;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
/// Represents an audit log entry stored in Azure Cosmos DB.
/// Captures all security-relevant events including PHI access.
/// This is an append-only collection with 6+ year retention.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for the audit entry.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the partition key (user ID or "system" for system events).
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public required string PartitionKey { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the event in UTC.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID who performed the action (null for system events).
    /// </summary>
    [JsonPropertyName("userId")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user's email at the time of the action.
    /// </summary>
    [JsonPropertyName("userEmail")]
    public string? UserEmail { get; set; }

    /// <summary>
    /// Gets or sets the action type (e.g., LOGIN_SUCCESS, PHI_ACCESSED).
    /// </summary>
    [JsonPropertyName("action")]
    public required string Action { get; set; }

    /// <summary>
    /// Gets or sets the type of resource being accessed (e.g., "Client", "Document").
    /// </summary>
    [JsonPropertyName("resourceType")]
    public string? ResourceType { get; set; }

    /// <summary>
    /// Gets or sets the ID of the specific resource accessed.
    /// </summary>
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; set; }

    /// <summary>
    /// Gets or sets the outcome of the action (Success, Failure, Denied).
    /// </summary>
    [JsonPropertyName("outcome")]
    public required string Outcome { get; set; }

    /// <summary>
    /// Gets or sets the client IP address.
    /// </summary>
    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets additional details about the action.
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method used (if applicable).
    /// </summary>
    [JsonPropertyName("httpMethod")]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Gets or sets the request path (if applicable).
    /// </summary>
    [JsonPropertyName("requestPath")]
    public string? RequestPath { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code (if applicable).
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for request tracing.
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Defines possible outcomes for audit log entries.
/// </summary>
public static class AuditOutcome
{
    public const string Success = "Success";
    public const string Failure = "Failure";
    public const string Denied = "Denied";
}
