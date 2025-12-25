using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
/// Represents a vital signs log entry.
/// Contains PHI that must be protected under HIPAA.
/// </summary>
public sealed class VitalsLog
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the client ID.
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Gets or sets the caregiver ID who recorded this entry.
    /// </summary>
    public Guid CaregiverId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when vitals were taken.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the systolic blood pressure (mmHg).
    /// </summary>
    public int? SystolicBP { get; set; }

    /// <summary>
    /// Gets or sets the diastolic blood pressure (mmHg).
    /// </summary>
    public int? DiastolicBP { get; set; }

    /// <summary>
    /// Gets or sets the pulse rate (beats per minute).
    /// </summary>
    public int? Pulse { get; set; }

    /// <summary>
    /// Gets or sets the temperature value.
    /// </summary>
    public decimal? Temperature { get; set; }

    /// <summary>
    /// Gets or sets the temperature unit (Fahrenheit or Celsius).
    /// </summary>
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Fahrenheit;

    /// <summary>
    /// Gets or sets the oxygen saturation percentage (SpO2).
    /// </summary>
    public int? OxygenSaturation { get; set; }

    /// <summary>
    /// Gets or sets optional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets when the record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the formatted blood pressure string.
    /// </summary>
    public string? BloodPressure => SystolicBP.HasValue && DiastolicBP.HasValue
        ? $"{SystolicBP}/{DiastolicBP}"
        : null;

    /// <summary>
    /// Navigation property for the client.
    /// </summary>
    public Client? Client { get; set; }

    /// <summary>
    /// Navigation property for the caregiver.
    /// </summary>
    public ApplicationUser? Caregiver { get; set; }
}
