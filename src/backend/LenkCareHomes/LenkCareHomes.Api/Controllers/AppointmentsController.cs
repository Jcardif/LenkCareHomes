using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Appointments;
using LenkCareHomes.Api.Services.Appointments;
using LenkCareHomes.Api.Services.Caregivers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
/// Controller for appointment management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ICaregiverService _caregiverService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentService appointmentService,
        ICaregiverService caregiverService,
        ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _caregiverService = caregiverService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all appointments with optional filters.
    /// Caregivers only see appointments for clients in their assigned homes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedAppointmentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedAppointmentResponse>> GetAppointmentsAsync(
        [FromQuery] Guid? clientId = null,
        [FromQuery] Guid? homeId = null,
        [FromQuery] AppointmentStatus? status = null,
        [FromQuery] AppointmentType? appointmentType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
            if (allowedHomeIds.Count == 0)
            {
                return Ok(new PagedAppointmentResponse
                {
                    Items = [],
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = 0,
                    HasNextPage = false,
                    HasPreviousPage = false
                });
            }
        }

        var result = await _appointmentService.GetAppointmentsAsync(
            clientId,
            homeId,
            status,
            appointmentType,
            startDate,
            endDate,
            allowedHomeIds,
            pageNumber,
            pageSize,
            sortDescending,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets an appointment by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetAppointmentByIdAsync))]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetAppointmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
        }

        var appointment = await _appointmentService.GetAppointmentByIdAsync(id, allowedHomeIds, cancellationToken);
        if (appointment is null)
        {
            // Check if exists but access denied
            var existsButDenied = await _appointmentService.GetAppointmentByIdAsync(id, null, cancellationToken);
            if (existsButDenied is not null)
            {
                return Forbid();
            }
            return NotFound();
        }

        return Ok(appointment);
    }

    /// <summary>
    /// Gets upcoming appointments for dashboard display.
    /// </summary>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(IReadOnlyList<UpcomingAppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UpcomingAppointmentDto>>> GetUpcomingAppointmentsAsync(
        [FromQuery] int days = 7,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
        }

        var appointments = await _appointmentService.GetUpcomingAppointmentsAsync(
            days,
            limit,
            allowedHomeIds,
            cancellationToken);

        return Ok(appointments);
    }

    /// <summary>
    /// Gets appointments for a specific client.
    /// </summary>
    [HttpGet("by-client/{clientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentSummaryDto>>> GetClientAppointmentsAsync(
        Guid clientId,
        [FromQuery] bool includeCompleted = true,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
        }

        var appointments = await _appointmentService.GetClientAppointmentsAsync(
            clientId,
            includeCompleted,
            allowedHomeIds,
            cancellationToken);

        return Ok(appointments);
    }

    /// <summary>
    /// Creates a new appointment.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppointmentOperationResponse>> CreateAppointmentAsync(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _appointmentService.CreateAppointmentAsync(
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(
            nameof(GetAppointmentByIdAsync),
            new { id = response.Appointment!.Id },
            response);
    }

    /// <summary>
    /// Updates an existing appointment.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentOperationResponse>> UpdateAppointmentAsync(
        Guid id,
        [FromBody] UpdateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
        }

        var response = await _appointmentService.UpdateAppointmentAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            allowedHomeIds,
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Appointment not found or access denied.")
            {
                var existsButDenied = await _appointmentService.GetAppointmentByIdAsync(id, null, cancellationToken);
                if (existsButDenied is not null)
                {
                    return Forbid();
                }
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Marks an appointment as completed.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentOperationResponse>> CompleteAppointmentAsync(
        Guid id,
        [FromBody] CompleteAppointmentRequest? request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
        }

        var response = await _appointmentService.CompleteAppointmentAsync(
            id,
            request ?? new CompleteAppointmentRequest(),
            currentUserId.Value,
            GetClientIpAddress(),
            allowedHomeIds,
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Appointment not found or access denied.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Cancels an appointment.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentOperationResponse>> CancelAppointmentAsync(
        Guid id,
        [FromBody] CancelAppointmentRequest? request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
        }

        var response = await _appointmentService.CancelAppointmentAsync(
            id,
            request ?? new CancelAppointmentRequest(),
            currentUserId.Value,
            GetClientIpAddress(),
            allowedHomeIds,
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Appointment not found or access denied.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Marks an appointment as no-show.
    /// </summary>
    [HttpPost("{id:guid}/no-show")]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentOperationResponse>> MarkNoShowAsync(
        Guid id,
        [FromBody] NoShowAppointmentRequest? request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
        }

        var response = await _appointmentService.MarkNoShowAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            allowedHomeIds,
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Appointment not found or access denied.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Reschedules an appointment to a new date/time.
    /// </summary>
    [HttpPost("{id:guid}/reschedule")]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentOperationResponse>> RescheduleAppointmentAsync(
        Guid id,
        [FromBody] RescheduleAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Sysadmin))
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
        }

        var response = await _appointmentService.RescheduleAppointmentAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            allowedHomeIds,
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Appointment not found or access denied.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Deletes an appointment (Admin only, only scheduled appointments).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppointmentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentOperationResponse>> DeleteAppointmentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _appointmentService.DeleteAppointmentAsync(
            id,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Appointment not found.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    private string? GetClientIpAddress()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
