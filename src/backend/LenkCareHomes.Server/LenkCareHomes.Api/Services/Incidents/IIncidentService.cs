using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Incidents;

namespace LenkCareHomes.Api.Services.Incidents;

/// <summary>
///     Service interface for incident reporting operations.
/// </summary>
public interface IIncidentService
{
    /// <summary>
    ///     Creates a new incident report.
    /// </summary>
    Task<IncidentOperationResponse> CreateIncidentAsync(
        CreateIncidentRequest request,
        Guid reportedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all incidents with optional filters.
    ///     Draft incidents are only visible to the user who created them.
    /// </summary>
    Task<PagedIncidentResponse> GetIncidentsAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid? homeId = null,
        IncidentStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        IncidentType? incidentType = null,
        Guid? clientId = null,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets an incident by ID.
    ///     Draft incidents are only accessible to the user who created them.
    /// </summary>
    Task<IncidentDto?> GetIncidentByIdAsync(
        Guid incidentId,
        Guid currentUserId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates a draft incident.
    ///     Only the creator can update their own drafts.
    /// </summary>
    Task<IncidentOperationResponse> UpdateIncidentAsync(
        Guid incidentId,
        UpdateIncidentRequest request,
        Guid currentUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Submits a draft incident for review.
    ///     Only the creator can submit their own drafts.
    /// </summary>
    Task<IncidentOperationResponse> SubmitIncidentAsync(
        Guid incidentId,
        Guid currentUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates incident status (Admin only).
    /// </summary>
    Task<IncidentOperationResponse> UpdateIncidentStatusAsync(
        Guid incidentId,
        UpdateIncidentStatusRequest request,
        Guid reviewedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds a follow-up note to an incident (Admin only).
    /// </summary>
    Task<IncidentOperationResponse> AddFollowUpAsync(
        Guid incidentId,
        AddIncidentFollowUpRequest request,
        Guid createdById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a draft incident.
    ///     Only the creator can delete their own drafts.
    /// </summary>
    Task<IncidentOperationResponse> DeleteIncidentAsync(
        Guid incidentId,
        Guid currentUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets count of recent new incidents (for dashboard).
    /// </summary>
    Task<int> GetRecentNewIncidentsCountAsync(
        int days = 7,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Initiates photo upload for an incident.
    ///     Returns SAS URL for direct upload to blob storage.
    /// </summary>
    Task<IncidentPhotoUploadResponse> InitiatePhotoUploadAsync(
        Guid incidentId,
        UploadIncidentPhotoRequest request,
        Guid uploadedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Confirms photo upload completed successfully.
    /// </summary>
    Task<IncidentPhotoOperationResponse> ConfirmPhotoUploadAsync(
        Guid photoId,
        Guid currentUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all photos for an incident.
    /// </summary>
    Task<IReadOnlyList<IncidentPhotoDto>> GetIncidentPhotosAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a SAS URL for viewing a photo.
    /// </summary>
    Task<IncidentPhotoViewResponse> GetPhotoViewUrlAsync(
        Guid photoId,
        Guid currentUserId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a photo from an incident.
    ///     Only incident author or admin can delete photos.
    /// </summary>
    Task<IncidentPhotoOperationResponse> DeletePhotoAsync(
        Guid photoId,
        Guid currentUserId,
        bool isAdmin,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Exports an incident report as PDF including all photos.
    /// </summary>
    Task<byte[]> ExportIncidentPdfAsync(
        Guid incidentId,
        Guid currentUserId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds,
        CancellationToken cancellationToken = default);
}