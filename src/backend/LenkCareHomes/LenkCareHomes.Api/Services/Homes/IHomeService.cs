using LenkCareHomes.Api.Models.Homes;

namespace LenkCareHomes.Api.Services.Homes;

/// <summary>
/// Service interface for home management operations.
/// </summary>
public interface IHomeService
{
    /// <summary>
    /// Gets all homes, optionally filtered by allowed home IDs (for caregivers).
    /// </summary>
    Task<IReadOnlyList<HomeSummaryDto>> GetAllHomesAsync(
        bool includeInactive = false,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a home by ID.
    /// </summary>
    Task<HomeDto?> GetHomeByIdAsync(Guid homeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new home.
    /// </summary>
    Task<HomeOperationResponse> CreateHomeAsync(
        CreateHomeRequest request,
        Guid createdById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing home.
    /// </summary>
    Task<HomeOperationResponse> UpdateHomeAsync(
        Guid homeId,
        UpdateHomeRequest request,
        Guid updatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a home.
    /// </summary>
    Task<HomeOperationResponse> DeactivateHomeAsync(
        Guid homeId,
        Guid deactivatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a home.
    /// </summary>
    Task<HomeOperationResponse> ReactivateHomeAsync(
        Guid homeId,
        Guid reactivatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
