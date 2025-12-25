namespace LenkCareHomes.Api.Domain.Enums;

/// <summary>
/// Status of an appointment.
/// </summary>
public enum AppointmentStatus
{
    /// <summary>
    /// Appointment is scheduled and upcoming.
    /// </summary>
    Scheduled,

    /// <summary>
    /// Appointment has been completed.
    /// </summary>
    Completed,

    /// <summary>
    /// Appointment was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Client did not show up for the appointment.
    /// </summary>
    NoShow,

    /// <summary>
    /// Appointment was rescheduled to a different time.
    /// </summary>
    Rescheduled
}
