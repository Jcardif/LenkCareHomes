using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Models.CareLog;
using LenkCareHomes.Api.Services.Caregivers;
using LenkCareHomes.Api.Services.CareLog;
using LenkCareHomes.Api.Services.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
/// Controller for client care logging operations (ADLs, Vitals, Medications, ROM, Notes).
/// </summary>
[ApiController]
[Route("api/clients/{clientId:guid}")]
[Authorize]
public sealed class CareLogController : ControllerBase
{
    private readonly IADLLogService _adlService;
    private readonly IVitalsLogService _vitalsService;
    private readonly IMedicationLogService _medicationService;
    private readonly IROMLogService _romService;
    private readonly IBehaviorNoteService _behaviorNoteService;
    private readonly ITimelineService _timelineService;
    private readonly ICaregiverService _caregiverService;
    private readonly IClientService _clientService;
    private readonly ILogger<CareLogController> _logger;

    public CareLogController(
        IADLLogService adlService,
        IVitalsLogService vitalsService,
        IMedicationLogService medicationService,
        IROMLogService romService,
        IBehaviorNoteService behaviorNoteService,
        ITimelineService timelineService,
        ICaregiverService caregiverService,
        IClientService clientService,
        ILogger<CareLogController> logger)
    {
        _adlService = adlService;
        _vitalsService = vitalsService;
        _medicationService = medicationService;
        _romService = romService;
        _behaviorNoteService = behaviorNoteService;
        _timelineService = timelineService;
        _caregiverService = caregiverService;
        _clientService = clientService;
        _logger = logger;
    }

    #region ADL Endpoints

    /// <summary>
    /// Creates a new ADL log entry for a client.
    /// </summary>
    [HttpPost("adls")]
    [ProducesResponseType(typeof(ADLLogOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ADLLogOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ADLLogOperationResponse>> CreateADLLogAsync(
        Guid clientId,
        [FromBody] CreateADLLogRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var currentUserId = GetCurrentUserId()!.Value;
        var response = await _adlService.CreateADLLogAsync(
            clientId, request, currentUserId, GetClientIpAddress(), cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(
            nameof(GetADLLogByIdAsync),
            new { clientId, adlId = response.ADLLog!.Id },
            response);
    }

    /// <summary>
    /// Gets ADL logs for a client.
    /// </summary>
    [HttpGet("adls")]
    [ProducesResponseType(typeof(IReadOnlyList<ADLLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ADLLogDto>>> GetADLLogsAsync(
        Guid clientId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var logs = await _adlService.GetADLLogsAsync(clientId, fromDate, toDate, cancellationToken);
        return Ok(logs);
    }

    /// <summary>
    /// Gets a specific ADL log by ID.
    /// </summary>
    [HttpGet("adls/{adlId:guid}")]
    [ActionName(nameof(GetADLLogByIdAsync))]
    [ProducesResponseType(typeof(ADLLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ADLLogDto>> GetADLLogByIdAsync(
        Guid clientId,
        Guid adlId,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var log = await _adlService.GetADLLogByIdAsync(clientId, adlId, cancellationToken);
        if (log is null)
        {
            return NotFound();
        }

        return Ok(log);
    }

    #endregion

    #region Vitals Endpoints

    /// <summary>
    /// Creates a new vitals log entry for a client.
    /// </summary>
    [HttpPost("vitals")]
    [ProducesResponseType(typeof(VitalsLogOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(VitalsLogOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VitalsLogOperationResponse>> CreateVitalsLogAsync(
        Guid clientId,
        [FromBody] CreateVitalsLogRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var currentUserId = GetCurrentUserId()!.Value;
        var response = await _vitalsService.CreateVitalsLogAsync(
            clientId, request, currentUserId, GetClientIpAddress(), cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(
            nameof(GetVitalsLogByIdAsync),
            new { clientId, vitalsId = response.VitalsLog!.Id },
            response);
    }

    /// <summary>
    /// Gets vitals logs for a client.
    /// </summary>
    [HttpGet("vitals")]
    [ProducesResponseType(typeof(IReadOnlyList<VitalsLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<VitalsLogDto>>> GetVitalsLogsAsync(
        Guid clientId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var logs = await _vitalsService.GetVitalsLogsAsync(clientId, fromDate, toDate, cancellationToken);
        return Ok(logs);
    }

    /// <summary>
    /// Gets a specific vitals log by ID.
    /// </summary>
    [HttpGet("vitals/{vitalsId:guid}")]
    [ActionName(nameof(GetVitalsLogByIdAsync))]
    [ProducesResponseType(typeof(VitalsLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VitalsLogDto>> GetVitalsLogByIdAsync(
        Guid clientId,
        Guid vitalsId,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var log = await _vitalsService.GetVitalsLogByIdAsync(clientId, vitalsId, cancellationToken);
        if (log is null)
        {
            return NotFound();
        }

        return Ok(log);
    }

    #endregion

    #region Medication Endpoints

    /// <summary>
    /// Creates a new medication log entry for a client.
    /// </summary>
    [HttpPost("medications")]
    [ProducesResponseType(typeof(MedicationLogOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MedicationLogOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MedicationLogOperationResponse>> CreateMedicationLogAsync(
        Guid clientId,
        [FromBody] CreateMedicationLogRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var currentUserId = GetCurrentUserId()!.Value;
        var response = await _medicationService.CreateMedicationLogAsync(
            clientId, request, currentUserId, GetClientIpAddress(), cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(
            nameof(GetMedicationLogByIdAsync),
            new { clientId, medicationId = response.MedicationLog!.Id },
            response);
    }

    /// <summary>
    /// Gets medication logs for a client.
    /// </summary>
    [HttpGet("medications")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicationLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<MedicationLogDto>>> GetMedicationLogsAsync(
        Guid clientId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var logs = await _medicationService.GetMedicationLogsAsync(clientId, fromDate, toDate, cancellationToken);
        return Ok(logs);
    }

    /// <summary>
    /// Gets a specific medication log by ID.
    /// </summary>
    [HttpGet("medications/{medicationId:guid}")]
    [ActionName(nameof(GetMedicationLogByIdAsync))]
    [ProducesResponseType(typeof(MedicationLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicationLogDto>> GetMedicationLogByIdAsync(
        Guid clientId,
        Guid medicationId,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var log = await _medicationService.GetMedicationLogByIdAsync(clientId, medicationId, cancellationToken);
        if (log is null)
        {
            return NotFound();
        }

        return Ok(log);
    }

    #endregion

    #region ROM Endpoints

    /// <summary>
    /// Creates a new ROM log entry for a client.
    /// </summary>
    [HttpPost("rom")]
    [ProducesResponseType(typeof(ROMLogOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ROMLogOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ROMLogOperationResponse>> CreateROMLogAsync(
        Guid clientId,
        [FromBody] CreateROMLogRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var currentUserId = GetCurrentUserId()!.Value;
        var response = await _romService.CreateROMLogAsync(
            clientId, request, currentUserId, GetClientIpAddress(), cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(
            nameof(GetROMLogByIdAsync),
            new { clientId, romId = response.ROMLog!.Id },
            response);
    }

    /// <summary>
    /// Gets ROM logs for a client.
    /// </summary>
    [HttpGet("rom")]
    [ProducesResponseType(typeof(IReadOnlyList<ROMLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ROMLogDto>>> GetROMLogsAsync(
        Guid clientId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var logs = await _romService.GetROMLogsAsync(clientId, fromDate, toDate, cancellationToken);
        return Ok(logs);
    }

    /// <summary>
    /// Gets a specific ROM log by ID.
    /// </summary>
    [HttpGet("rom/{romId:guid}")]
    [ActionName(nameof(GetROMLogByIdAsync))]
    [ProducesResponseType(typeof(ROMLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ROMLogDto>> GetROMLogByIdAsync(
        Guid clientId,
        Guid romId,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var log = await _romService.GetROMLogByIdAsync(clientId, romId, cancellationToken);
        if (log is null)
        {
            return NotFound();
        }

        return Ok(log);
    }

    #endregion

    #region Behavior Notes Endpoints

    /// <summary>
    /// Creates a new behavior note for a client.
    /// </summary>
    [HttpPost("behavior-notes")]
    [ProducesResponseType(typeof(BehaviorNoteOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BehaviorNoteOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BehaviorNoteOperationResponse>> CreateBehaviorNoteAsync(
        Guid clientId,
        [FromBody] CreateBehaviorNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var currentUserId = GetCurrentUserId()!.Value;
        var response = await _behaviorNoteService.CreateBehaviorNoteAsync(
            clientId, request, currentUserId, GetClientIpAddress(), cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(
            nameof(GetBehaviorNoteByIdAsync),
            new { clientId, noteId = response.BehaviorNote!.Id },
            response);
    }

    /// <summary>
    /// Gets behavior notes for a client.
    /// </summary>
    [HttpGet("behavior-notes")]
    [ProducesResponseType(typeof(IReadOnlyList<BehaviorNoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BehaviorNoteDto>>> GetBehaviorNotesAsync(
        Guid clientId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var notes = await _behaviorNoteService.GetBehaviorNotesAsync(clientId, fromDate, toDate, cancellationToken);
        return Ok(notes);
    }

    /// <summary>
    /// Gets a specific behavior note by ID.
    /// </summary>
    [HttpGet("behavior-notes/{noteId:guid}")]
    [ActionName(nameof(GetBehaviorNoteByIdAsync))]
    [ProducesResponseType(typeof(BehaviorNoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BehaviorNoteDto>> GetBehaviorNoteByIdAsync(
        Guid clientId,
        Guid noteId,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var note = await _behaviorNoteService.GetBehaviorNoteByIdAsync(clientId, noteId, cancellationToken);
        if (note is null)
        {
            return NotFound();
        }

        return Ok(note);
    }

    #endregion

    #region Timeline Endpoints

    /// <summary>
    /// Gets a unified timeline of all care activities for a client.
    /// </summary>
    [HttpGet("timeline")]
    [ProducesResponseType(typeof(TimelineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TimelineResponse>> GetClientTimelineAsync(
        Guid clientId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? entryTypes = null,
        [FromQuery] int pageSize = 50,
        [FromQuery] int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeClientAccessAsync(clientId, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var queryParams = new TimelineQueryParams
        {
            FromDate = fromDate,
            ToDate = toDate,
            EntryTypes = string.IsNullOrWhiteSpace(entryTypes)
                ? null
                : entryTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            PageSize = Math.Clamp(pageSize, 1, 100),
            PageNumber = Math.Max(1, pageNumber)
        };

        var timeline = await _timelineService.GetClientTimelineAsync(clientId, queryParams, cancellationToken);
        return Ok(timeline);
    }

    #endregion

    #region Helper Methods

    private async Task<ActionResult?> AuthorizeClientAccessAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        // Admins and Sysadmins have full access
        if (User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Sysadmin))
        {
            return null;
        }

        // For caregivers, check if client is in their assigned homes
        var allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
        var client = await _clientService.GetClientByIdAsync(clientId, allowedHomeIds, cancellationToken);

        if (client is null)
        {
            // Check if client exists but user doesn't have access
            var existsButDenied = await _clientService.GetClientByIdAsync(clientId, null, cancellationToken);
            if (existsButDenied is not null)
            {
                return Forbid();
            }
            return NotFound();
        }

        return null;
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

    #endregion
}
