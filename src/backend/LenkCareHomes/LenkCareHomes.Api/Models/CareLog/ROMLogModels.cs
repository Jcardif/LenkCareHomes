namespace LenkCareHomes.Api.Models.CareLog;

/// <summary>
/// DTO for ROM log information.
/// </summary>
public sealed record ROMLogDto
{
    public required Guid Id { get; init; }
    public required Guid ClientId { get; init; }
    public required Guid CaregiverId { get; init; }
    public required string CaregiverName { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string ActivityDescription { get; init; }
    public int? Duration { get; init; }
    public int? Repetitions { get; init; }
    public string? Notes { get; init; }
    public required DateTime CreatedAt { get; init; }
}

/// <summary>
/// Request model for creating a ROM log entry.
/// </summary>
public sealed record CreateROMLogRequest
{
    public DateTime? Timestamp { get; init; }
    public required string ActivityDescription { get; init; }
    public int? Duration { get; init; }
    public int? Repetitions { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Response model for ROM log operations.
/// </summary>
public sealed record ROMLogOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public ROMLogDto? ROMLog { get; init; }
}
