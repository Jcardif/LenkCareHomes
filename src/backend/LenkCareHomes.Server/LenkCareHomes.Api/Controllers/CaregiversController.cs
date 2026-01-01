using System.Security.Claims;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Models.Caregivers;
using LenkCareHomes.Api.Services.Caregivers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
///     Controller for caregiver management and home assignment operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
public sealed class CaregiversController : ControllerBase
{
    private readonly ICaregiverService _caregiverService;
    private readonly ILogger<CaregiversController> _logger;

    public CaregiversController(
        ICaregiverService caregiverService,
        ILogger<CaregiversController> logger)
    {
        _caregiverService = caregiverService;
        _logger = logger;
    }

    /// <summary>
    ///     Gets all caregivers.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive caregivers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of caregivers.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CaregiverSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CaregiverSummaryDto>>> GetAllCaregiversAsync(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var caregivers = await _caregiverService.GetAllCaregiversAsync(includeInactive, cancellationToken);
        return Ok(caregivers);
    }

    /// <summary>
    ///     Gets all caregivers assigned to a specific home.
    /// </summary>
    /// <param name="homeId">The home ID to filter caregivers by.</param>
    /// <param name="includeInactive">Whether to include inactive caregivers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of caregivers assigned to the specified home.</returns>
    [HttpGet("by-home/{homeId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<CaregiverSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CaregiverSummaryDto>>> GetCaregiversByHomeAsync(
        Guid homeId,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var caregivers = await _caregiverService.GetCaregiversByHomeAsync(homeId, includeInactive, cancellationToken);
        return Ok(caregivers);
    }

    /// <summary>
    ///     Gets a caregiver by ID with their home assignments.
    /// </summary>
    /// <param name="id">Caregiver ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Caregiver details with home assignments.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CaregiverDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaregiverDto>> GetCaregiverByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var caregiver = await _caregiverService.GetCaregiverByIdAsync(id, cancellationToken);
        if (caregiver is null) return NotFound();

        return Ok(caregiver);
    }

    /// <summary>
    ///     Assigns homes to a caregiver.
    /// </summary>
    /// <param name="id">Caregiver ID.</param>
    /// <param name="request">Assign homes request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated caregiver.</returns>
    [HttpPost("{id:guid}/assign-homes")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(CaregiverOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CaregiverOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaregiverOperationResponse>> AssignHomesAsync(
        Guid id,
        [FromBody] AssignHomesRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _caregiverService.AssignHomesAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Caregiver not found." || response.Error == "User is not a caregiver.")
                return NotFound();
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Removes a home assignment from a caregiver.
    /// </summary>
    /// <param name="id">Caregiver ID.</param>
    /// <param name="homeId">Home ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated caregiver.</returns>
    [HttpDelete("{id:guid}/homes/{homeId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(CaregiverOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaregiverOperationResponse>> RemoveHomeAssignmentAsync(
        Guid id,
        Guid homeId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _caregiverService.RemoveHomeAssignmentAsync(
            id,
            homeId,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Home assignment not found.") return NotFound();
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
}