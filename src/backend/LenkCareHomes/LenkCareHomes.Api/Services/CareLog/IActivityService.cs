using LenkCareHomes.Api.Models.CareLog;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
/// Service interface for activity operations.
/// </summary>
public interface IActivityService
{
    /// <summary>
    /// Creates a new activity.
    /// </summary>
    Task<ActivityOperationResponse> CreateActivityAsync(
        CreateActivityRequest request,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an activity (admin only).
    /// </summary>
    Task<ActivityOperationResponse> UpdateActivityAsync(
        Guid activityId,
        UpdateActivityRequest request,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an activity (admin only).
    /// </summary>
    Task<ActivityOperationResponse> DeleteActivityAsync(
        Guid activityId,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an activity by ID.
    /// </summary>
    Task<ActivityDto?> GetActivityByIdAsync(
        Guid activityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities for a client.
    /// </summary>
    Task<IReadOnlyList<ActivityDto>> GetActivitiesByClientAsync(
        Guid clientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities for a home.
    /// </summary>
    Task<IReadOnlyList<ActivityDto>> GetActivitiesByHomeAsync(
        Guid homeId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}
