using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Models.Incidents;

/// <summary>
///     DTO for incident summary in list views.
/// </summary>
public sealed record IncidentSummaryDto
{
    public Guid Id { get; init; }
    public string IncidentNumber { get; init; } = string.Empty;
    public Guid? ClientId { get; init; }
    public string? ClientName { get; init; }
    public Guid HomeId { get; init; }
    public string HomeName { get; init; } = string.Empty;
    public IncidentType IncidentType { get; init; }
    public int Severity { get; init; }
    public DateTime OccurredAt { get; init; }
    public IncidentStatus Status { get; init; }
    public string ReportedByName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
///     DTO for full incident details.
/// </summary>
public sealed record IncidentDto
{
    public Guid Id { get; init; }
    public string IncidentNumber { get; init; } = string.Empty;
    public Guid? ClientId { get; init; }
    public string? ClientName { get; init; }
    public Guid HomeId { get; init; }
    public string HomeName { get; init; } = string.Empty;
    public Guid ReportedById { get; init; }
    public string ReportedByName { get; init; } = string.Empty;
    public IncidentType IncidentType { get; init; }
    public int Severity { get; init; }
    public DateTime OccurredAt { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ActionsTaken { get; init; }
    public string? WitnessNames { get; init; }
    public string? NotifiedParties { get; init; }
    public DateTime? AdminNotifiedAt { get; init; }
    public IncidentStatus Status { get; init; }
    public Guid? ClosedById { get; init; }
    public string? ClosedByName { get; init; }
    public DateTime? ClosedAt { get; init; }
    public string? ClosureNotes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<IncidentFollowUpDto> FollowUps { get; init; } = [];
    public IReadOnlyList<IncidentPhotoDto> Photos { get; init; } = [];
}

/// <summary>
///     DTO for incident follow-up notes.
/// </summary>
public sealed record IncidentFollowUpDto
{
    public Guid Id { get; init; }
    public Guid CreatedById { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
///     DTO for incident photos.
/// </summary>
public sealed record IncidentPhotoDto
{
    public Guid Id { get; init; }
    public Guid IncidentId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public int DisplayOrder { get; init; }
    public string? Caption { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
}

/// <summary>
///     Request to initiate photo upload for an incident.
/// </summary>
public sealed record UploadIncidentPhotoRequest
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string? Caption { get; init; }
}

/// <summary>
///     Response from photo upload initiation.
/// </summary>
public sealed record IncidentPhotoUploadResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Guid? PhotoId { get; init; }
    public string? UploadUrl { get; init; }
    public DateTime? ExpiresAt { get; init; }

    public static IncidentPhotoUploadResponse Ok(Guid photoId, string uploadUrl, DateTime expiresAt)
    {
        return new IncidentPhotoUploadResponse
            { Success = true, PhotoId = photoId, UploadUrl = uploadUrl, ExpiresAt = expiresAt };
    }

    public static IncidentPhotoUploadResponse Fail(string error)
    {
        return new IncidentPhotoUploadResponse { Success = false, Error = error };
    }
}

/// <summary>
///     Response from photo operations.
/// </summary>
public sealed record IncidentPhotoOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public IncidentPhotoDto? Photo { get; init; }

    public static IncidentPhotoOperationResponse Ok(IncidentPhotoDto photo)
    {
        return new IncidentPhotoOperationResponse { Success = true, Photo = photo };
    }

    public static IncidentPhotoOperationResponse Fail(string error)
    {
        return new IncidentPhotoOperationResponse { Success = false, Error = error };
    }
}

/// <summary>
///     Response with photo URL for viewing.
/// </summary>
public sealed record IncidentPhotoViewResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? Url { get; init; }
    public DateTime? ExpiresAt { get; init; }

    public static IncidentPhotoViewResponse Ok(string url, DateTime expiresAt)
    {
        return new IncidentPhotoViewResponse { Success = true, Url = url, ExpiresAt = expiresAt };
    }

    public static IncidentPhotoViewResponse Fail(string error)
    {
        return new IncidentPhotoViewResponse { Success = false, Error = error };
    }
}

/// <summary>
///     Request to create a new incident report.
/// </summary>
public sealed record CreateIncidentRequest
{
    /// <summary>
    ///     Optional client ID. If null, the incident is a home-level incident.
    /// </summary>
    public Guid? ClientId { get; init; }

    public Guid HomeId { get; init; }
    public IncidentType IncidentType { get; init; }
    public int Severity { get; init; } = 3;
    public DateTime OccurredAt { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ActionsTaken { get; init; }
    public string? WitnessNames { get; init; }
    public string? NotifiedParties { get; init; }

    /// <summary>
    ///     If true, the incident is submitted immediately (status = Submitted).
    ///     If false, it's saved as a draft (status = Draft).
    /// </summary>
    public bool SubmitImmediately { get; init; }
}

/// <summary>
///     Request to update an existing draft incident.
/// </summary>
public sealed record UpdateIncidentRequest
{
    public IncidentType? IncidentType { get; init; }
    public int? Severity { get; init; }
    public DateTime? OccurredAt { get; init; }
    public string? Location { get; init; }
    public string? Description { get; init; }
    public string? ActionsTaken { get; init; }
    public string? WitnessNames { get; init; }
    public string? NotifiedParties { get; init; }
}

/// <summary>
///     Request to update incident status.
/// </summary>
public sealed record UpdateIncidentStatusRequest
{
    public IncidentStatus NewStatus { get; init; }
    public string? ClosureNotes { get; init; }
}

/// <summary>
///     Request to add a follow-up note to an incident.
/// </summary>
public sealed record AddIncidentFollowUpRequest
{
    public string Note { get; init; } = string.Empty;
}

/// <summary>
///     Response from incident operations.
/// </summary>
public sealed record IncidentOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public IncidentDto? Incident { get; init; }

    public static IncidentOperationResponse Ok(IncidentDto incident)
    {
        return new IncidentOperationResponse { Success = true, Incident = incident };
    }

    public static IncidentOperationResponse Fail(string error)
    {
        return new IncidentOperationResponse { Success = false, Error = error };
    }
}

/// <summary>
///     Paged response for incidents list.
/// </summary>
public sealed record PagedIncidentResponse
{
    public IReadOnlyList<IncidentSummaryDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }

    public static PagedIncidentResponse Create(
        IReadOnlyList<IncidentSummaryDto> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedIncidentResponse
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            HasPreviousPage = pageNumber > 1
        };
    }
}