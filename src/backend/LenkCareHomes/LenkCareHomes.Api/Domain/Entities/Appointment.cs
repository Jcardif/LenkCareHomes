using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents an appointment for a client (resident) in an adult family home.
///     Contains PHI that must be protected under HIPAA.
/// </summary>
public sealed class Appointment
{
    /// <summary>
    ///     Gets or sets the unique identifier for the appointment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the client this appointment is for.
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    ///     Gets or sets the home associated with the appointment (via the client).
    /// </summary>
    public Guid HomeId { get; set; }

    /// <summary>
    ///     Gets or sets the type of appointment.
    /// </summary>
    public AppointmentType AppointmentType { get; set; }

    /// <summary>
    ///     Gets or sets the status of the appointment.
    /// </summary>
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    /// <summary>
    ///     Gets or sets the title or short description of the appointment.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    ///     Gets or sets the scheduled date and time of the appointment.
    /// </summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>
    ///     Gets or sets the expected duration of the appointment in minutes.
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    ///     Gets or sets the location or address of the appointment.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    ///     Gets or sets the provider name (doctor, therapist, etc.).
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    ///     Gets or sets the provider's phone number.
    /// </summary>
    public string? ProviderPhone { get; set; }

    /// <summary>
    ///     Gets or sets additional notes about the appointment.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    ///     Gets or sets notes about transportation arrangements.
    /// </summary>
    public string? TransportationNotes { get; set; }

    /// <summary>
    ///     Gets or sets whether a reminder has been sent.
    /// </summary>
    public bool ReminderSent { get; set; }

    /// <summary>
    ///     Gets or sets the user who created this appointment.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    ///     Gets or sets when the appointment was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets when the appointment was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     Gets or sets outcome notes after the appointment is completed.
    /// </summary>
    public string? OutcomeNotes { get; set; }

    /// <summary>
    ///     Gets or sets the user who marked the appointment as completed.
    /// </summary>
    public Guid? CompletedById { get; set; }

    /// <summary>
    ///     Gets or sets when the appointment was marked as completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    ///     Navigation property for the client.
    /// </summary>
    public Client? Client { get; set; }

    /// <summary>
    ///     Navigation property for the home.
    /// </summary>
    public Home? Home { get; set; }

    /// <summary>
    ///     Navigation property for the user who created the appointment.
    /// </summary>
    public ApplicationUser? CreatedBy { get; set; }

    /// <summary>
    ///     Navigation property for the user who completed the appointment.
    /// </summary>
    public ApplicationUser? CompletedBy { get; set; }
}