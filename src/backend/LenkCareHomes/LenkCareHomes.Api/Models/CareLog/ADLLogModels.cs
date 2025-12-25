using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Models.CareLog;

/// <summary>
/// DTO for ADL log information.
/// </summary>
public sealed record ADLLogDto
{
    public required Guid Id { get; init; }
    public required Guid ClientId { get; init; }
    public required Guid CaregiverId { get; init; }
    public required string CaregiverName { get; init; }
    public required DateTime Timestamp { get; init; }
    public ADLLevel? Bathing { get; init; }
    public ADLLevel? Dressing { get; init; }
    public ADLLevel? Toileting { get; init; }
    public ADLLevel? Transferring { get; init; }
    public ADLLevel? Continence { get; init; }
    public ADLLevel? Feeding { get; init; }
    public string? Notes { get; init; }
    public int KatzScore { get; init; }
    public required DateTime CreatedAt { get; init; }
}

/// <summary>
/// Request model for creating an ADL log entry.
/// </summary>
public sealed record CreateADLLogRequest
{
    public DateTime? Timestamp { get; init; }
    public ADLLevel? Bathing { get; init; }
    public ADLLevel? Dressing { get; init; }
    public ADLLevel? Toileting { get; init; }
    public ADLLevel? Transferring { get; init; }
    public ADLLevel? Continence { get; init; }
    public ADLLevel? Feeding { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Response model for ADL log operations.
/// </summary>
public sealed record ADLLogOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public ADLLogDto? ADLLog { get; init; }
}
