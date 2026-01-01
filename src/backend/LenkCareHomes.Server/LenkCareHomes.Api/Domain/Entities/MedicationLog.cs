using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents a medication administration log entry.
///     Contains PHI that must be protected under HIPAA.
/// </summary>
public sealed class MedicationLog
{
    /// <summary>
    ///     Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the client ID.
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    ///     Gets or sets the caregiver ID who recorded this entry.
    /// </summary>
    public Guid CaregiverId { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when medication was administered.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     Gets or sets the medication name.
    /// </summary>
    public required string MedicationName { get; set; }

    /// <summary>
    ///     Gets or sets the dosage (e.g., "10mg", "2 tablets", "5ml").
    /// </summary>
    public required string Dosage { get; set; }

    /// <summary>
    ///     Gets or sets the route of administration.
    /// </summary>
    public MedicationRoute Route { get; set; } = MedicationRoute.Oral;

    /// <summary>
    ///     Gets or sets the administration status.
    /// </summary>
    public MedicationStatus Status { get; set; } = MedicationStatus.Administered;

    /// <summary>
    ///     Gets or sets the scheduled time for this medication (if applicable).
    /// </summary>
    public DateTime? ScheduledTime { get; set; }

    /// <summary>
    ///     Gets or sets the prescribing physician's name (optional).
    /// </summary>
    public string? PrescribedBy { get; set; }

    /// <summary>
    ///     Gets or sets the pharmacy name (optional).
    /// </summary>
    public string? Pharmacy { get; set; }

    /// <summary>
    ///     Gets or sets the prescription number (optional).
    /// </summary>
    public string? RxNumber { get; set; }

    /// <summary>
    ///     Gets or sets optional notes about the administration.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    ///     Gets or sets when the record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Navigation property for the client.
    /// </summary>
    public Client? Client { get; set; }

    /// <summary>
    ///     Navigation property for the caregiver.
    /// </summary>
    public ApplicationUser? Caregiver { get; set; }
}