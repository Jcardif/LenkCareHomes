using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Models.CareLog;

/// <summary>
/// DTO for behavior note information.
/// </summary>
public sealed record BehaviorNoteDto
{
    public required Guid Id { get; init; }
    public required Guid ClientId { get; init; }
    public required Guid CaregiverId { get; init; }
    public required string CaregiverName { get; init; }
    public required DateTime Timestamp { get; init; }
    public required BehaviorCategory Category { get; init; }
    public required string NoteText { get; init; }
    public NoteSeverity? Severity { get; init; }
    public required DateTime CreatedAt { get; init; }
}

/// <summary>
/// Request model for creating a behavior note.
/// </summary>
public sealed record CreateBehaviorNoteRequest
{
    public DateTime? Timestamp { get; init; }
    public required BehaviorCategory Category { get; init; }
    public required string NoteText { get; init; }
    public NoteSeverity? Severity { get; init; }
}

/// <summary>
/// Response model for behavior note operations.
/// </summary>
public sealed record BehaviorNoteOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public BehaviorNoteDto? BehaviorNote { get; init; }
}
