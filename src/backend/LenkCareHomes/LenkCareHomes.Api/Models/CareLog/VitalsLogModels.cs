using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Models.CareLog;

/// <summary>
/// DTO for vitals log information.
/// </summary>
public sealed record VitalsLogDto
{
    public required Guid Id { get; init; }
    public required Guid ClientId { get; init; }
    public required Guid CaregiverId { get; init; }
    public required string CaregiverName { get; init; }
    public required DateTime Timestamp { get; init; }
    public int? SystolicBP { get; init; }
    public int? DiastolicBP { get; init; }
    public string? BloodPressure { get; init; }
    public int? Pulse { get; init; }
    public decimal? Temperature { get; init; }
    public TemperatureUnit TemperatureUnit { get; init; }
    public int? OxygenSaturation { get; init; }
    public string? Notes { get; init; }
    public required DateTime CreatedAt { get; init; }
}

/// <summary>
/// Request model for creating a vitals log entry.
/// </summary>
public sealed record CreateVitalsLogRequest
{
    public DateTime? Timestamp { get; init; }
    public int? SystolicBP { get; init; }
    public int? DiastolicBP { get; init; }
    public int? Pulse { get; init; }
    public decimal? Temperature { get; init; }
    public TemperatureUnit? TemperatureUnit { get; init; }
    public int? OxygenSaturation { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Response model for vitals log operations.
/// </summary>
public sealed record VitalsLogOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public VitalsLogDto? VitalsLog { get; init; }
}
