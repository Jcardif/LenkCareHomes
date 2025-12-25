namespace LenkCareHomes.Api.Models.CareLog;

/// <summary>
/// Represents a unified care timeline entry.
/// </summary>
public sealed record TimelineEntryDto
{
    public required Guid Id { get; init; }
    public required string EntryType { get; init; }
    public required DateTime Timestamp { get; init; }
    public required Guid CaregiverId { get; init; }
    public required string CaregiverName { get; init; }
    public required string Summary { get; init; }
    public object? Details { get; init; }
    public required DateTime CreatedAt { get; init; }
}

/// <summary>
/// Timeline entry types.
/// </summary>
public static class TimelineEntryTypes
{
    public const string ADL = "ADL";
    public const string Vitals = "Vitals";
    public const string Medication = "Medication";
    public const string ROM = "ROM";
    public const string BehaviorNote = "BehaviorNote";
    public const string Activity = "Activity";
}

/// <summary>
/// Query parameters for timeline.
/// </summary>
public sealed record TimelineQueryParams
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public IReadOnlyList<string>? EntryTypes { get; init; }
    public int PageSize { get; init; } = 50;
    public int PageNumber { get; init; } = 1;
}

/// <summary>
/// Timeline response with pagination.
/// </summary>
public sealed record TimelineResponse
{
    public required IReadOnlyList<TimelineEntryDto> Entries { get; init; }
    public required int TotalCount { get; init; }
    public required int PageSize { get; init; }
    public required int PageNumber { get; init; }
    public required int TotalPages { get; init; }
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}
