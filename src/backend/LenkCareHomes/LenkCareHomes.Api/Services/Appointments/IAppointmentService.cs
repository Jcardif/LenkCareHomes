using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Appointments;

namespace LenkCareHomes.Api.Services.Appointments;

/// <summary>
/// Service interface for appointment operations.
/// </summary>
public interface IAppointmentService
{
    /// <summary>
    /// Creates a new appointment.
    /// </summary>
    Task<AppointmentOperationResponse> CreateAppointmentAsync(
        CreateAppointmentRequest request,
        Guid createdById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets appointments with optional filters.
    /// Caregivers only see appointments for clients in their assigned homes.
    /// </summary>
    Task<PagedAppointmentResponse> GetAppointmentsAsync(
        Guid? clientId = null,
        Guid? homeId = null,
        AppointmentStatus? status = null,
        AppointmentType? appointmentType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        int pageNumber = 1,
        int pageSize = 10,
        bool sortDescending = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an appointment by ID.
    /// </summary>
    Task<AppointmentDto?> GetAppointmentByIdAsync(
        Guid appointmentId,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing appointment.
    /// </summary>
    Task<AppointmentOperationResponse> UpdateAppointmentAsync(
        Guid appointmentId,
        UpdateAppointmentRequest request,
        Guid updatedById,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an appointment as completed.
    /// </summary>
    Task<AppointmentOperationResponse> CompleteAppointmentAsync(
        Guid appointmentId,
        CompleteAppointmentRequest request,
        Guid completedById,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an appointment.
    /// </summary>
    Task<AppointmentOperationResponse> CancelAppointmentAsync(
        Guid appointmentId,
        CancelAppointmentRequest request,
        Guid cancelledById,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an appointment as no-show.
    /// </summary>
    Task<AppointmentOperationResponse> MarkNoShowAsync(
        Guid appointmentId,
        NoShowAppointmentRequest? request,
        Guid markedById,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reschedules an appointment (marks current as Rescheduled).
    /// </summary>
    Task<AppointmentOperationResponse> RescheduleAppointmentAsync(
        Guid appointmentId,
        RescheduleAppointmentRequest request,
        Guid rescheduledById,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an appointment (Admin only, only scheduled appointments).
    /// </summary>
    Task<AppointmentOperationResponse> DeleteAppointmentAsync(
        Guid appointmentId,
        Guid deletedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets upcoming appointments for dashboard display.
    /// </summary>
    Task<IReadOnlyList<UpcomingAppointmentDto>> GetUpcomingAppointmentsAsync(
        int days = 7,
        int limit = 10,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets appointments for a specific client.
    /// </summary>
    Task<IReadOnlyList<AppointmentSummaryDto>> GetClientAppointmentsAsync(
        Guid clientId,
        bool includeCompleted = true,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);
}
