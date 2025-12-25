using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Models.CareLog;
using LenkCareHomes.Api.Services.CareLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
/// Controller for recreational and group activity operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ActivitiesController : ControllerBase
{
    private readonly IActivityService _activityService;
    private readonly ILogger<ActivitiesController> _logger;

    public ActivitiesController(
        IActivityService activityService,
        ILogger<ActivitiesController> logger)
    {
        _activityService = activityService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new activity (individual or group).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ActivityOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ActivityOperationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ActivityOperationResponse>> CreateActivityAsync(
        [FromBody] CreateActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _activityService.CreateActivityAsync(
            request, currentUserId.Value, GetClientIpAddress(), cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(
            nameof(GetActivityByIdAsync),
            new { id = response.Activity!.Id },
            response);
    }

    /// <summary>
    /// Gets an activity by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetActivityByIdAsync))]
    [ProducesResponseType(typeof(ActivityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityDto>> GetActivityByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var activity = await _activityService.GetActivityByIdAsync(id, cancellationToken);
        if (activity is null)
        {
            return NotFound();
        }

        return Ok(activity);
    }

    /// <summary>
    /// Updates an activity (admin only).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ActivityOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActivityOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityOperationResponse>> UpdateActivityAsync(
        Guid id,
        [FromBody] UpdateActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _activityService.UpdateActivityAsync(
            id, request, currentUserId.Value, GetClientIpAddress(), cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Activity not found.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Deletes an activity (admin only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ActivityOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityOperationResponse>> DeleteActivityAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _activityService.DeleteActivityAsync(
            id, currentUserId.Value, GetClientIpAddress(), cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Activity not found.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets activities by home.
    /// </summary>
    [HttpGet("by-home/{homeId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ActivityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ActivityDto>>> GetActivitiesByHomeAsync(
        Guid homeId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var activities = await _activityService.GetActivitiesByHomeAsync(homeId, fromDate, toDate, cancellationToken);
        return Ok(activities);
    }

    /// <summary>
    /// Gets activities by client.
    /// </summary>
    [HttpGet("by-client/{clientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ActivityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ActivityDto>>> GetActivitiesByClientAsync(
        Guid clientId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var activities = await _activityService.GetActivitiesByClientAsync(clientId, fromDate, toDate, cancellationToken);
        return Ok(activities);
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
