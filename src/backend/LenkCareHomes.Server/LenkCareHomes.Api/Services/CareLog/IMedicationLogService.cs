using LenkCareHomes.Api.Models.CareLog;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
///     Service interface for medication log operations.
/// </summary>
public interface IMedicationLogService
{
    /// <summary>
    ///     Creates a new medication log entry.
    /// </summary>
    Task<MedicationLogOperationResponse> CreateMedicationLogAsync(
        Guid clientId,
        CreateMedicationLogRequest request,
        Guid caregiverId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets medication logs for a client.
    /// </summary>
    Task<IReadOnlyList<MedicationLogDto>> GetMedicationLogsAsync(
        Guid clientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a specific medication log by ID.
    /// </summary>
    Task<MedicationLogDto?> GetMedicationLogByIdAsync(
        Guid clientId,
        Guid medicationId,
        CancellationToken cancellationToken = default);
}