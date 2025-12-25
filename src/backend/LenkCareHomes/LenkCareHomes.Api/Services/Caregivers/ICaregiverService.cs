using LenkCareHomes.Api.Models.Caregivers;

namespace LenkCareHomes.Api.Services.Caregivers;

/// <summary>
/// Service interface for caregiver management and home assignment operations.
/// </summary>
public interface ICaregiverService
{
    /// <summary>
    /// Gets all caregivers.
    /// </summary>
    Task<IReadOnlyList<CaregiverSummaryDto>> GetAllCaregiversAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a caregiver by ID with their home assignments.
    /// </summary>
    Task<CaregiverDto?> GetCaregiverByIdAsync(Guid caregiverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns homes to a caregiver.
    /// </summary>
    Task<CaregiverOperationResponse> AssignHomesAsync(
        Guid caregiverId,
        AssignHomesRequest request,
        Guid assignedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a home assignment from a caregiver.
    /// </summary>
    Task<CaregiverOperationResponse> RemoveHomeAssignmentAsync(
        Guid caregiverId,
        Guid homeId,
        Guid removedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the home IDs assigned to a caregiver.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAssignedHomeIdsAsync(
        Guid caregiverId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all caregivers assigned to a specific home.
    /// </summary>
    /// <param name="homeId">The home ID to filter caregivers by.</param>
    /// <param name="includeInactive">Whether to include inactive caregivers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of caregivers assigned to the specified home.</returns>
    Task<IReadOnlyList<CaregiverSummaryDto>> GetCaregiversByHomeAsync(
        Guid homeId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}
