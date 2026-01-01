using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Appointments;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.Appointments;

/// <summary>
///     Service for appointment operations.
/// </summary>
public sealed class AppointmentService : IAppointmentService
{
    private readonly IAuditLogService _auditLogService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        ILogger<AppointmentService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AppointmentOperationResponse> CreateAppointmentAsync(
        CreateAppointmentRequest request,
        Guid createdById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate client exists and get their home ID
        var client = await _context.Clients
            .AsNoTracking()
            .Where(c => c.Id == request.ClientId && c.IsActive)
            .Select(c => new { c.Id, c.HomeId, c.FirstName, c.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        if (client is null)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Client not found or is not active."
            };

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            ClientId = request.ClientId,
            HomeId = client.HomeId,
            AppointmentType = request.AppointmentType,
            Status = AppointmentStatus.Scheduled,
            Title = request.Title,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            Location = request.Location,
            ProviderName = request.ProviderName,
            ProviderPhone = request.ProviderPhone,
            Notes = request.Notes,
            TransportationNotes = request.TransportationNotes,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        // Get user email for audit log
        var createdBy = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == createdById)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        // Log to audit
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.AppointmentCreated,
            createdById,
            createdBy ?? "Unknown",
            "Appointment",
            appointment.Id.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Appointment '{request.Title}' scheduled for client {client.FirstName} {client.LastName} on {request.ScheduledAt:g}",
            cancellationToken);

        var dto = await GetAppointmentByIdAsync(appointment.Id, null, cancellationToken);

        return new AppointmentOperationResponse
        {
            Success = true,
            Appointment = dto
        };
    }

    /// <inheritdoc />
    public async Task<PagedAppointmentResponse> GetAppointmentsAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Client)
            .Include(a => a.Home)
            .AsQueryable();

        // Apply home-scope for caregivers
        if (allowedHomeIds is not null) query = query.Where(a => allowedHomeIds.Contains(a.HomeId));

        if (clientId.HasValue) query = query.Where(a => a.ClientId == clientId.Value);

        if (homeId.HasValue) query = query.Where(a => a.HomeId == homeId.Value);

        if (status.HasValue) query = query.Where(a => a.Status == status.Value);

        if (appointmentType.HasValue) query = query.Where(a => a.AppointmentType == appointmentType.Value);

        if (startDate.HasValue) query = query.Where(a => a.ScheduledAt >= startDate.Value);

        if (endDate.HasValue) query = query.Where(a => a.ScheduledAt <= endDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        // Apply sort order - descending shows most recent first
        var orderedQuery = sortDescending
            ? query.OrderByDescending(a => a.ScheduledAt)
            : query.OrderBy(a => a.ScheduledAt);

        var appointments = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AppointmentSummaryDto
            {
                Id = a.Id,
                ClientId = a.ClientId,
                ClientName = a.Client != null ? $"{a.Client.FirstName} {a.Client.LastName}" : "Unknown",
                HomeId = a.HomeId,
                HomeName = a.Home != null ? a.Home.Name : "Unknown",
                AppointmentType = a.AppointmentType,
                Status = a.Status,
                Title = a.Title,
                ScheduledAt = a.ScheduledAt,
                DurationMinutes = a.DurationMinutes,
                Location = a.Location,
                ProviderName = a.ProviderName
            })
            .ToListAsync(cancellationToken);

        return new PagedAppointmentResponse
        {
            Items = appointments,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            HasPreviousPage = pageNumber > 1
        };
    }

    /// <inheritdoc />
    public async Task<AppointmentDto?> GetAppointmentByIdAsync(
        Guid appointmentId,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Client)
            .Include(a => a.Home)
            .Include(a => a.CreatedBy)
            .Include(a => a.CompletedBy)
            .Where(a => a.Id == appointmentId);

        if (allowedHomeIds is not null) query = query.Where(a => allowedHomeIds.Contains(a.HomeId));

        var appointment = await query.FirstOrDefaultAsync(cancellationToken);

        if (appointment is null) return null;

        return new AppointmentDto
        {
            Id = appointment.Id,
            ClientId = appointment.ClientId,
            ClientName = appointment.Client != null
                ? $"{appointment.Client.FirstName} {appointment.Client.LastName}"
                : "Unknown",
            HomeId = appointment.HomeId,
            HomeName = appointment.Home?.Name ?? "Unknown",
            AppointmentType = appointment.AppointmentType,
            Status = appointment.Status,
            Title = appointment.Title,
            ScheduledAt = appointment.ScheduledAt,
            DurationMinutes = appointment.DurationMinutes,
            Location = appointment.Location,
            ProviderName = appointment.ProviderName,
            ProviderPhone = appointment.ProviderPhone,
            Notes = appointment.Notes,
            TransportationNotes = appointment.TransportationNotes,
            ReminderSent = appointment.ReminderSent,
            CreatedById = appointment.CreatedById,
            CreatedByName = appointment.CreatedBy != null
                ? $"{appointment.CreatedBy.FirstName} {appointment.CreatedBy.LastName}"
                : "Unknown",
            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt,
            OutcomeNotes = appointment.OutcomeNotes,
            CompletedById = appointment.CompletedById,
            CompletedByName = appointment.CompletedBy != null
                ? $"{appointment.CompletedBy.FirstName} {appointment.CompletedBy.LastName}"
                : null,
            CompletedAt = appointment.CompletedAt
        };
    }

    /// <inheritdoc />
    public async Task<AppointmentOperationResponse> UpdateAppointmentAsync(
        Guid appointmentId,
        UpdateAppointmentRequest request,
        Guid updatedById,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = _context.Appointments.Where(a => a.Id == appointmentId);

        if (allowedHomeIds is not null) query = query.Where(a => allowedHomeIds.Contains(a.HomeId));

        var appointment = await query.FirstOrDefaultAsync(cancellationToken);

        if (appointment is null)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Appointment not found or access denied."
            };

        if (appointment.Status != AppointmentStatus.Scheduled)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Only scheduled appointments can be updated."
            };

        // Update fields
        if (request.AppointmentType.HasValue) appointment.AppointmentType = request.AppointmentType.Value;

        if (!string.IsNullOrWhiteSpace(request.Title)) appointment.Title = request.Title;

        if (request.ScheduledAt.HasValue) appointment.ScheduledAt = request.ScheduledAt.Value;

        if (request.DurationMinutes.HasValue) appointment.DurationMinutes = request.DurationMinutes.Value;

        if (request.Location is not null) appointment.Location = request.Location;

        if (request.ProviderName is not null) appointment.ProviderName = request.ProviderName;

        if (request.ProviderPhone is not null) appointment.ProviderPhone = request.ProviderPhone;

        if (request.Notes is not null) appointment.Notes = request.Notes;

        if (request.TransportationNotes is not null) appointment.TransportationNotes = request.TransportationNotes;

        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Get user email for audit log
        var updatedBy = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == updatedById)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        await _auditLogService.LogPhiAccessAsync(
            AuditActions.AppointmentUpdated,
            updatedById,
            updatedBy ?? "Unknown",
            "Appointment",
            appointment.Id.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Appointment '{appointment.Title}' updated",
            cancellationToken);

        var dto = await GetAppointmentByIdAsync(appointment.Id, null, cancellationToken);

        return new AppointmentOperationResponse
        {
            Success = true,
            Appointment = dto
        };
    }

    /// <inheritdoc />
    public async Task<AppointmentOperationResponse> CompleteAppointmentAsync(
        Guid appointmentId,
        CompleteAppointmentRequest request,
        Guid completedById,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments.Where(a => a.Id == appointmentId);

        if (allowedHomeIds is not null) query = query.Where(a => allowedHomeIds.Contains(a.HomeId));

        var appointment = await query.FirstOrDefaultAsync(cancellationToken);

        if (appointment is null)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Appointment not found or access denied."
            };

        if (appointment.Status != AppointmentStatus.Scheduled)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Only scheduled appointments can be completed."
            };

        appointment.Status = AppointmentStatus.Completed;
        appointment.OutcomeNotes = request?.OutcomeNotes;
        appointment.CompletedById = completedById;
        appointment.CompletedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Get user email for audit log
        var completedBy = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == completedById)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        await _auditLogService.LogPhiAccessAsync(
            AuditActions.AppointmentCompleted,
            completedById,
            completedBy ?? "Unknown",
            "Appointment",
            appointment.Id.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Appointment '{appointment.Title}' completed",
            cancellationToken);

        var dto = await GetAppointmentByIdAsync(appointment.Id, null, cancellationToken);

        return new AppointmentOperationResponse
        {
            Success = true,
            Appointment = dto
        };
    }

    /// <inheritdoc />
    public async Task<AppointmentOperationResponse> CancelAppointmentAsync(
        Guid appointmentId,
        CancelAppointmentRequest request,
        Guid cancelledById,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments.Where(a => a.Id == appointmentId);

        if (allowedHomeIds is not null) query = query.Where(a => allowedHomeIds.Contains(a.HomeId));

        var appointment = await query.FirstOrDefaultAsync(cancellationToken);

        if (appointment is null)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Appointment not found or access denied."
            };

        if (appointment.Status != AppointmentStatus.Scheduled)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Only scheduled appointments can be cancelled."
            };

        appointment.Status = AppointmentStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(request?.CancellationReason))
            appointment.Notes = string.IsNullOrWhiteSpace(appointment.Notes)
                ? $"Cancelled: {request.CancellationReason}"
                : $"{appointment.Notes}\n\nCancelled: {request.CancellationReason}";
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Get user email for audit log
        var cancelledBy = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == cancelledById)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        await _auditLogService.LogPhiAccessAsync(
            AuditActions.AppointmentCancelled,
            cancelledById,
            cancelledBy ?? "Unknown",
            "Appointment",
            appointment.Id.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Appointment '{appointment.Title}' cancelled",
            cancellationToken);

        var dto = await GetAppointmentByIdAsync(appointment.Id, null, cancellationToken);

        return new AppointmentOperationResponse
        {
            Success = true,
            Appointment = dto
        };
    }

    /// <inheritdoc />
    public async Task<AppointmentOperationResponse> MarkNoShowAsync(
        Guid appointmentId,
        NoShowAppointmentRequest? request,
        Guid markedById,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments.Where(a => a.Id == appointmentId);

        if (allowedHomeIds is not null) query = query.Where(a => allowedHomeIds.Contains(a.HomeId));

        var appointment = await query.FirstOrDefaultAsync(cancellationToken);

        if (appointment is null)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Appointment not found or access denied."
            };

        if (appointment.Status != AppointmentStatus.Scheduled)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Only scheduled appointments can be marked as no-show."
            };

        appointment.Status = AppointmentStatus.NoShow;
        if (!string.IsNullOrWhiteSpace(request?.Notes))
            appointment.Notes = string.IsNullOrWhiteSpace(appointment.Notes)
                ? $"No-show: {request.Notes}"
                : $"{appointment.Notes}\n\nNo-show: {request.Notes}";
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Get user email for audit log
        var markedBy = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == markedById)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        await _auditLogService.LogPhiAccessAsync(
            AuditActions.AppointmentNoShow,
            markedById,
            markedBy ?? "Unknown",
            "Appointment",
            appointment.Id.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Appointment '{appointment.Title}' marked as no-show",
            cancellationToken);

        var dto = await GetAppointmentByIdAsync(appointment.Id, null, cancellationToken);

        return new AppointmentOperationResponse
        {
            Success = true,
            Appointment = dto
        };
    }

    /// <inheritdoc />
    public async Task<AppointmentOperationResponse> RescheduleAppointmentAsync(
        Guid appointmentId,
        RescheduleAppointmentRequest request,
        Guid rescheduledById,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = _context.Appointments.Where(a => a.Id == appointmentId);

        if (allowedHomeIds is not null) query = query.Where(a => allowedHomeIds.Contains(a.HomeId));

        var appointment = await query.FirstOrDefaultAsync(cancellationToken);

        if (appointment is null)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Appointment not found or access denied."
            };

        if (appointment.Status != AppointmentStatus.Scheduled)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Only scheduled appointments can be rescheduled."
            };

        var previousDate = appointment.ScheduledAt;
        appointment.Status = AppointmentStatus.Rescheduled;
        appointment.ScheduledAt = request.NewScheduledAt;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            appointment.Notes = string.IsNullOrWhiteSpace(appointment.Notes)
                ? $"Rescheduled from {previousDate:g}: {request.Notes}"
                : $"{appointment.Notes}\n\nRescheduled from {previousDate:g}: {request.Notes}";
        else
            appointment.Notes = string.IsNullOrWhiteSpace(appointment.Notes)
                ? $"Rescheduled from {previousDate:g}"
                : $"{appointment.Notes}\n\nRescheduled from {previousDate:g}";
        // Reset status to Scheduled after rescheduling since it's a new scheduled time
        appointment.Status = AppointmentStatus.Scheduled;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Get user email for audit log
        var rescheduledBy = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == rescheduledById)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        await _auditLogService.LogPhiAccessAsync(
            AuditActions.AppointmentRescheduled,
            rescheduledById,
            rescheduledBy ?? "Unknown",
            "Appointment",
            appointment.Id.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Appointment '{appointment.Title}' rescheduled from {previousDate:g} to {request.NewScheduledAt:g}",
            cancellationToken);

        var dto = await GetAppointmentByIdAsync(appointment.Id, null, cancellationToken);

        return new AppointmentOperationResponse
        {
            Success = true,
            Appointment = dto
        };
    }

    /// <inheritdoc />
    public async Task<AppointmentOperationResponse> DeleteAppointmentAsync(
        Guid appointmentId,
        Guid deletedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Appointment not found."
            };

        if (appointment.Status != AppointmentStatus.Scheduled)
            return new AppointmentOperationResponse
            {
                Success = false,
                Error = "Only scheduled appointments can be deleted."
            };

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        // Get user email for audit log
        var deletedBy = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == deletedById)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        await _auditLogService.LogPhiAccessAsync(
            AuditActions.AppointmentDeleted,
            deletedById,
            deletedBy ?? "Unknown",
            "Appointment",
            appointmentId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Appointment '{appointment.Title}' deleted",
            cancellationToken);

        return new AppointmentOperationResponse { Success = true };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UpcomingAppointmentDto>> GetUpcomingAppointmentsAsync(
        int days = 7,
        int limit = 10,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var endDate = now.AddDays(days);

        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Client)
            .Include(a => a.Home)
            .Where(a => a.Status == AppointmentStatus.Scheduled)
            .Where(a => a.ScheduledAt >= now && a.ScheduledAt <= endDate);

        if (allowedHomeIds is not null) query = query.Where(a => allowedHomeIds.Contains(a.HomeId));

        return await query
            .OrderBy(a => a.ScheduledAt)
            .Take(limit)
            .Select(a => new UpcomingAppointmentDto
            {
                Id = a.Id,
                ClientId = a.ClientId,
                ClientName = a.Client != null ? $"{a.Client.FirstName} {a.Client.LastName}" : "Unknown",
                HomeName = a.Home != null ? a.Home.Name : "Unknown",
                AppointmentType = a.AppointmentType,
                Title = a.Title,
                ScheduledAt = a.ScheduledAt,
                Location = a.Location,
                ProviderName = a.ProviderName
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AppointmentSummaryDto>> GetClientAppointmentsAsync(
        Guid clientId,
        bool includeCompleted = true,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Client)
            .Include(a => a.Home)
            .Where(a => a.ClientId == clientId);

        if (allowedHomeIds is not null) query = query.Where(a => allowedHomeIds.Contains(a.HomeId));

        if (!includeCompleted) query = query.Where(a => a.Status == AppointmentStatus.Scheduled);

        return await query
            .OrderByDescending(a => a.ScheduledAt)
            .Select(a => new AppointmentSummaryDto
            {
                Id = a.Id,
                ClientId = a.ClientId,
                ClientName = a.Client != null ? $"{a.Client.FirstName} {a.Client.LastName}" : "Unknown",
                HomeId = a.HomeId,
                HomeName = a.Home != null ? a.Home.Name : "Unknown",
                AppointmentType = a.AppointmentType,
                Status = a.Status,
                Title = a.Title,
                ScheduledAt = a.ScheduledAt,
                DurationMinutes = a.DurationMinutes,
                Location = a.Location,
                ProviderName = a.ProviderName
            })
            .ToListAsync(cancellationToken);
    }
}