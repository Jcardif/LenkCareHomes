using LenkCareHomes.Api.Models.CareLog;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
/// Service interface for ADL log operations.
/// </summary>
public interface IADLLogService
{
    /// <summary>
    /// Creates a new ADL log entry.
    /// </summary>
    Task<ADLLogOperationResponse> CreateADLLogAsync(
        Guid clientId,
        CreateADLLogRequest request,
        Guid caregiverId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ADL logs for a client.
    /// </summary>
    Task<IReadOnlyList<ADLLogDto>> GetADLLogsAsync(
        Guid clientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific ADL log by ID.
    /// </summary>
    Task<ADLLogDto?> GetADLLogByIdAsync(
        Guid clientId,
        Guid adlId,
        CancellationToken cancellationToken = default);
}
