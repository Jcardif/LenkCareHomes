using LenkCareHomes.Api.Models.CareLog;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
/// Service interface for ROM log operations.
/// </summary>
public interface IROMLogService
{
    /// <summary>
    /// Creates a new ROM log entry.
    /// </summary>
    Task<ROMLogOperationResponse> CreateROMLogAsync(
        Guid clientId,
        CreateROMLogRequest request,
        Guid caregiverId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ROM logs for a client.
    /// </summary>
    Task<IReadOnlyList<ROMLogDto>> GetROMLogsAsync(
        Guid clientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific ROM log by ID.
    /// </summary>
    Task<ROMLogDto?> GetROMLogByIdAsync(
        Guid clientId,
        Guid romId,
        CancellationToken cancellationToken = default);
}
