using LenkCareHomes.Api.Models.CareLog;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
/// Service interface for vitals log operations.
/// </summary>
public interface IVitalsLogService
{
    /// <summary>
    /// Creates a new vitals log entry.
    /// </summary>
    Task<VitalsLogOperationResponse> CreateVitalsLogAsync(
        Guid clientId,
        CreateVitalsLogRequest request,
        Guid caregiverId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets vitals logs for a client.
    /// </summary>
    Task<IReadOnlyList<VitalsLogDto>> GetVitalsLogsAsync(
        Guid clientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific vitals log by ID.
    /// </summary>
    Task<VitalsLogDto?> GetVitalsLogByIdAsync(
        Guid clientId,
        Guid vitalsId,
        CancellationToken cancellationToken = default);
}
