using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Models.Appointments;

/// <summary>
///     DTO for appointment summary in list views.
/// </summary>
public sealed record AppointmentSummaryDto
{
    public required Guid Id { get; init; }
    public required Guid ClientId { get; init; }
    public required string ClientName { get; init; }
    public required Guid HomeId { get; init; }
    public required string HomeName { get; init; }
    public required AppointmentType AppointmentType { get; init; }
    public required AppointmentStatus Status { get; init; }
    public required string Title { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public int? DurationMinutes { get; init; }
    public string? Location { get; init; }
    public string? ProviderName { get; init; }
}

/// <summary>
///     Full DTO for appointment details.
/// </summary>
public sealed record AppointmentDto
{
    public required Guid Id { get; init; }
    public required Guid ClientId { get; init; }
    public required string ClientName { get; init; }
    public required Guid HomeId { get; init; }
    public required string HomeName { get; init; }
    public required AppointmentType AppointmentType { get; init; }
    public required AppointmentStatus Status { get; init; }
    public required string Title { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public int? DurationMinutes { get; init; }
    public string? Location { get; init; }
    public string? ProviderName { get; init; }
    public string? ProviderPhone { get; init; }
    public string? Notes { get; init; }
    public string? TransportationNotes { get; init; }
    public bool ReminderSent { get; init; }
    public required Guid CreatedById { get; init; }
    public required string CreatedByName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? OutcomeNotes { get; init; }
    public Guid? CompletedById { get; init; }
    public string? CompletedByName { get; init; }
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
///     Request to create a new appointment.
/// </summary>
public sealed record CreateAppointmentRequest
{
    public required Guid ClientId { get; init; }
    public required AppointmentType AppointmentType { get; init; }
    public required string Title { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public int? DurationMinutes { get; init; }
    public string? Location { get; init; }
    public string? ProviderName { get; init; }
    public string? ProviderPhone { get; init; }
    public string? Notes { get; init; }
    public string? TransportationNotes { get; init; }
}

/// <summary>
///     Request to update an existing appointment.
/// </summary>
public sealed record UpdateAppointmentRequest
{
    public AppointmentType? AppointmentType { get; init; }
    public string? Title { get; init; }
    public DateTime? ScheduledAt { get; init; }
    public int? DurationMinutes { get; init; }
    public string? Location { get; init; }
    public string? ProviderName { get; init; }
    public string? ProviderPhone { get; init; }
    public string? Notes { get; init; }
    public string? TransportationNotes { get; init; }
}

/// <summary>
///     Request to complete an appointment.
/// </summary>
public sealed record CompleteAppointmentRequest
{
    public string? OutcomeNotes { get; init; }
}

/// <summary>
///     Request to cancel an appointment.
/// </summary>
public sealed record CancelAppointmentRequest
{
    public string? CancellationReason { get; init; }
}

/// <summary>
///     Request to mark an appointment as no-show.
/// </summary>
public sealed record NoShowAppointmentRequest
{
    public string? Notes { get; init; }
}

/// <summary>
///     Request to reschedule an appointment.
/// </summary>
public sealed record RescheduleAppointmentRequest
{
    public required DateTime NewScheduledAt { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
///     Response for appointment operations.
/// </summary>
public sealed record AppointmentOperationResponse
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public AppointmentDto? Appointment { get; init; }
}

/// <summary>
///     Paged response for appointments.
/// </summary>
public sealed record PagedAppointmentResponse
{
    public required IReadOnlyList<AppointmentSummaryDto> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
    public required bool HasNextPage { get; init; }
    public required bool HasPreviousPage { get; init; }
}

/// <summary>
///     DTO for upcoming appointment display on dashboard.
/// </summary>
public sealed record UpcomingAppointmentDto
{
    public required Guid Id { get; init; }
    public required Guid ClientId { get; init; }
    public required string ClientName { get; init; }
    public required string HomeName { get; init; }
    public required AppointmentType AppointmentType { get; init; }
    public required string Title { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public string? Location { get; init; }
    public string? ProviderName { get; init; }
}