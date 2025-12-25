using System.Security.Claims;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Incidents;
using LenkCareHomes.Api.Services.Caregivers;
using LenkCareHomes.Api.Services.Incidents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
///     Controller for incident reporting and management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class IncidentsController : ControllerBase
{
    private readonly ICaregiverService _caregiverService;
    private readonly IIncidentService _incidentService;
    private readonly ILogger<IncidentsController> _logger;

    public IncidentsController(
        IIncidentService incidentService,
        ICaregiverService caregiverService,
        ILogger<IncidentsController> logger)
    {
        _incidentService = incidentService;
        _caregiverService = caregiverService;
        _logger = logger;
    }

    /// <summary>
    ///     Gets all incidents with optional filters.
    ///     Caregivers only see incidents from their assigned homes.
    ///     Draft incidents are only visible to the user who created them.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedIncidentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedIncidentResponse>> GetIncidentsAsync(
        [FromQuery] Guid? homeId = null,
        [FromQuery] IncidentStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] IncidentType? incidentType = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Admin);
        var isSysadmin = User.IsInRole(Roles.Sysadmin);

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !isSysadmin)
        {
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);

            if (allowedHomeIds.Count == 0) return Ok(PagedIncidentResponse.Create([], 0, pageNumber, pageSize));
        }

        var result = await _incidentService.GetIncidentsAsync(
            currentUserId.Value,
            isAdmin,
            homeId,
            status,
            startDate,
            endDate,
            incidentType,
            clientId,
            allowedHomeIds,
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    ///     Gets an incident by ID.
    ///     Caregivers can only access incidents from their assigned homes.
    ///     Draft incidents are only accessible to the user who created them.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetIncidentByIdAsync))]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDto>> GetIncidentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Admin);

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !User.IsInRole(Roles.Sysadmin))
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);

        var incident =
            await _incidentService.GetIncidentByIdAsync(id, currentUserId.Value, isAdmin, allowedHomeIds,
                cancellationToken);
        if (incident is null)
        {
            // Check if incident exists but user doesn't have access
            var existsButDenied =
                await _incidentService.GetIncidentByIdAsync(id, currentUserId.Value, true, null, cancellationToken);
            if (existsButDenied is not null) return Forbid();
            return NotFound();
        }

        return Ok(incident);
    }

    /// <summary>
    ///     Creates a new incident report.
    ///     Available to both Admins and Caregivers.
    ///     Caregivers can only create incidents for homes they are assigned to.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Caregiver}")]
    [ProducesResponseType(typeof(IncidentOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IncidentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IncidentOperationResponse>> CreateIncidentAsync(
        [FromBody] CreateIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Admin);

        // Caregivers can only create incidents for homes they are assigned to
        if (!isAdmin)
        {
            var allowedHomeIds =
                await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
            if (!allowedHomeIds.Contains(request.HomeId)) return Forbid();
        }

        var response = await _incidentService.CreateIncidentAsync(
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success) return BadRequest(response);

        return CreatedAtAction(
            nameof(GetIncidentByIdAsync),
            new { id = response.Incident!.Id },
            response);
    }

    /// <summary>
    ///     Updates a draft incident.
    ///     Only the creator can update their own draft.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Caregiver}")]
    [ProducesResponseType(typeof(IncidentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IncidentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentOperationResponse>> UpdateIncidentAsync(
        Guid id,
        [FromBody] UpdateIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _incidentService.UpdateIncidentAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Incident not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Submits a draft incident for review.
    ///     Only the creator can submit their own draft.
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Caregiver}")]
    [ProducesResponseType(typeof(IncidentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IncidentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentOperationResponse>> SubmitIncidentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _incidentService.SubmitIncidentAsync(
            id,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Incident not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Deletes a draft incident.
    ///     Only the creator can delete their own draft.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Caregiver}")]
    [ProducesResponseType(typeof(IncidentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IncidentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentOperationResponse>> DeleteIncidentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _incidentService.DeleteIncidentAsync(
            id,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Incident not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Updates incident status.
    ///     Admin only.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IncidentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IncidentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentOperationResponse>> UpdateIncidentStatusAsync(
        Guid id,
        [FromBody] UpdateIncidentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _incidentService.UpdateIncidentStatusAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Incident not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Adds a follow-up note to an incident.
    ///     Admin only.
    /// </summary>
    [HttpPost("{id:guid}/follow-up")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IncidentOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IncidentOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentOperationResponse>> AddFollowUpAsync(
        Guid id,
        [FromBody] AddIncidentFollowUpRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _incidentService.AddFollowUpAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Incident not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    private string? GetClientIpAddress()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor)) return forwardedFor.Split(',').First().Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId)) return userId;
        return null;
    }

    #region Photo Management

    /// <summary>
    ///     Gets all photos for an incident.
    /// </summary>
    [HttpGet("{id:guid}/photos")]
    [ProducesResponseType(typeof(IReadOnlyList<IncidentPhotoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<IncidentPhotoDto>>> GetIncidentPhotosAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Admin);

        // Check access
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !User.IsInRole(Roles.Sysadmin))
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);

        var incident =
            await _incidentService.GetIncidentByIdAsync(id, currentUserId.Value, isAdmin, allowedHomeIds,
                cancellationToken);
        if (incident is null) return NotFound();

        var photos = await _incidentService.GetIncidentPhotosAsync(id, cancellationToken);
        return Ok(photos);
    }

    /// <summary>
    ///     Initiates a photo upload for an incident.
    ///     Returns a pre-signed URL for uploading directly to blob storage.
    /// </summary>
    [HttpPost("{id:guid}/photos")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Caregiver}")]
    [ProducesResponseType(typeof(IncidentPhotoUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IncidentPhotoUploadResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentPhotoUploadResponse>> InitiatePhotoUploadAsync(
        Guid id,
        [FromBody] UploadIncidentPhotoRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Admin);

        // Verify access to incident
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin)
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);

        var incident =
            await _incidentService.GetIncidentByIdAsync(id, currentUserId.Value, isAdmin, allowedHomeIds,
                cancellationToken);
        if (incident is null) return NotFound();

        var response = await _incidentService.InitiatePhotoUploadAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success) return BadRequest(response);

        return Created($"/api/incidents/{id}/photos/{response.PhotoId}", response);
    }

    /// <summary>
    ///     Confirms a photo upload has completed.
    ///     Should be called after successfully uploading to the pre-signed URL.
    /// </summary>
    [HttpPost("{id:guid}/photos/{photoId:guid}/confirm")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Caregiver}")]
    [ProducesResponseType(typeof(IncidentPhotoOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IncidentPhotoOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentPhotoOperationResponse>> ConfirmPhotoUploadAsync(
        Guid id,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _incidentService.ConfirmPhotoUploadAsync(
            photoId,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Photo not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Gets a pre-signed URL for viewing a photo.
    /// </summary>
    [HttpGet("{id:guid}/photos/{photoId:guid}/view")]
    [ProducesResponseType(typeof(IncidentPhotoViewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IncidentPhotoViewResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentPhotoViewResponse>> GetPhotoViewUrlAsync(
        Guid id,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Admin);

        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !User.IsInRole(Roles.Sysadmin))
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);

        var response = await _incidentService.GetPhotoViewUrlAsync(
            photoId,
            currentUserId.Value,
            isAdmin,
            allowedHomeIds,
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Photo not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Deletes a photo from an incident.
    ///     Only the incident author or admin can delete photos.
    /// </summary>
    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Caregiver}")]
    [ProducesResponseType(typeof(IncidentPhotoOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IncidentPhotoOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentPhotoOperationResponse>> DeletePhotoAsync(
        Guid id,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Admin);

        var response = await _incidentService.DeletePhotoAsync(
            photoId,
            currentUserId.Value,
            isAdmin,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Photo not found.") return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Exports an incident report as PDF.
    ///     Includes all incident details and attached photos.
    /// </summary>
    [HttpGet("{id:guid}/export/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportIncidentPdfAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var isAdmin = User.IsInRole(Roles.Admin);

        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !User.IsInRole(Roles.Sysadmin))
            allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);

        try
        {
            var pdfBytes = await _incidentService.ExportIncidentPdfAsync(
                id,
                currentUserId.Value,
                isAdmin,
                allowedHomeIds,
                cancellationToken);

            var incident = await _incidentService.GetIncidentByIdAsync(id, currentUserId.Value, isAdmin, allowedHomeIds,
                cancellationToken);
            var fileName = $"Incident-Report-{incident?.IncidentNumber ?? id.ToString()}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Incident not found.")
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    #endregion
}