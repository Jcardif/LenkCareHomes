using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Models.CareLog;

/// <summary>
///     DTO for activity information.
/// </summary>
public sealed record ActivityDto
{
    public required Guid Id { get; init; }
    public required string ActivityName { get; init; }
    public string? Description { get; init; }
    public required DateTime Date { get; init; }
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public int? Duration { get; init; }
    public required ActivityCategory Category { get; init; }
    public required bool IsGroupActivity { get; init; }
    public Guid? HomeId { get; init; }
    public string? HomeName { get; init; }
    public required Guid CreatedById { get; init; }
    public required string CreatedByName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<ActivityParticipantDto> Participants { get; init; } = [];
}

/// <summary>
///     DTO for activity participant information.
/// </summary>
public sealed record ActivityParticipantDto
{
    public required Guid ClientId { get; init; }
    public required string ClientName { get; init; }
}

/// <summary>
///     Request model for creating an activity.
/// </summary>
public sealed record CreateActivityRequest
{
    public required string ActivityName { get; init; }
    public string? Description { get; init; }
    public required DateTime Date { get; init; }
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public int? Duration { get; init; }
    public required ActivityCategory Category { get; init; }
    public bool IsGroupActivity { get; init; }
    public Guid? HomeId { get; init; }
    public required IReadOnlyList<Guid> ClientIds { get; init; }
}

/// <summary>
///     Request model for updating an activity.
/// </summary>
public sealed record UpdateActivityRequest
{
    public string? ActivityName { get; init; }
    public string? Description { get; init; }
    public DateTime? Date { get; init; }
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public int? Duration { get; init; }
    public ActivityCategory? Category { get; init; }
    public bool? IsGroupActivity { get; init; }
    public IReadOnlyList<Guid>? ClientIds { get; init; }
}

/// <summary>
///     Response model for activity operations.
/// </summary>
public sealed record ActivityOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public ActivityDto? Activity { get; init; }
}