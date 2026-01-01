using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Models.CareLog;

/// <summary>
///     DTO for medication log information.
/// </summary>
public sealed record MedicationLogDto
{
    public required Guid Id { get; init; }
    public required Guid ClientId { get; init; }
    public required Guid CaregiverId { get; init; }
    public required string CaregiverName { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string MedicationName { get; init; }
    public required string Dosage { get; init; }
    public required MedicationRoute Route { get; init; }
    public required MedicationStatus Status { get; init; }
    public DateTime? ScheduledTime { get; init; }
    public string? PrescribedBy { get; init; }
    public string? Pharmacy { get; init; }
    public string? RxNumber { get; init; }
    public string? Notes { get; init; }
    public required DateTime CreatedAt { get; init; }
}

/// <summary>
///     Request model for creating a medication log entry.
/// </summary>
public sealed record CreateMedicationLogRequest
{
    public DateTime? Timestamp { get; init; }
    public required string MedicationName { get; init; }
    public required string Dosage { get; init; }
    public MedicationRoute Route { get; init; } = MedicationRoute.Oral;
    public MedicationStatus Status { get; init; } = MedicationStatus.Administered;
    public DateTime? ScheduledTime { get; init; }
    public string? PrescribedBy { get; init; }
    public string? Pharmacy { get; init; }
    public string? RxNumber { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
///     Response model for medication log operations.
/// </summary>
public sealed record MedicationLogOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public MedicationLogDto? MedicationLog { get; init; }
}