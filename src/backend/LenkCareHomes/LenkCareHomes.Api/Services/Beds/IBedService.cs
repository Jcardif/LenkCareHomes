using LenkCareHomes.Api.Models.Beds;

namespace LenkCareHomes.Api.Services.Beds;

/// <summary>
/// Service interface for bed management operations.
/// </summary>
public interface IBedService
{
    /// <summary>
    /// Gets all beds for a home.
    /// </summary>
    Task<IReadOnlyList<BedDto>> GetBedsByHomeIdAsync(
        Guid homeId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a bed by ID.
    /// </summary>
    Task<BedDto?> GetBedByIdAsync(Guid bedId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new bed in a home.
    /// </summary>
    Task<BedOperationResponse> CreateBedAsync(
        Guid homeId,
        CreateBedRequest request,
        Guid createdById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing bed.
    /// </summary>
    Task<BedOperationResponse> UpdateBedAsync(
        Guid bedId,
        UpdateBedRequest request,
        Guid updatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available beds for a home.
    /// </summary>
    Task<IReadOnlyList<BedDto>> GetAvailableBedsAsync(
        Guid homeId,
        CancellationToken cancellationToken = default);
}
