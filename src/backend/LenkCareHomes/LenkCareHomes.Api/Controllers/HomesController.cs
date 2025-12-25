using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Models.Beds;
using LenkCareHomes.Api.Models.Homes;
using LenkCareHomes.Api.Services.Beds;
using LenkCareHomes.Api.Services.Caregivers;
using LenkCareHomes.Api.Services.Homes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
/// Controller for home management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class HomesController : ControllerBase
{
    private readonly IHomeService _homeService;
    private readonly IBedService _bedService;
    private readonly ICaregiverService _caregiverService;
    private readonly ILogger<HomesController> _logger;

    public HomesController(
        IHomeService homeService,
        IBedService bedService,
        ICaregiverService caregiverService,
        ILogger<HomesController> logger)
    {
        _homeService = homeService;
        _bedService = bedService;
        _caregiverService = caregiverService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all homes.
    /// Caregivers only see homes they are assigned to.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive homes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of homes.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HomeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HomeSummaryDto>>> GetAllHomesAsync(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var isAdmin = User.IsInRole(Roles.Admin);
        var isSysadmin = User.IsInRole(Roles.Sysadmin);

        // Get home scope for caregivers
        IReadOnlyList<Guid>? allowedHomeIds = null;
        if (!isAdmin && !isSysadmin)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId is not null)
            {
                allowedHomeIds = await _caregiverService.GetAssignedHomeIdsAsync(currentUserId.Value, cancellationToken);
            }
        }

        var homes = await _homeService.GetAllHomesAsync(includeInactive, allowedHomeIds, cancellationToken);
        return Ok(homes);
    }

    /// <summary>
    /// Gets a home by ID.
    /// </summary>
    /// <param name="id">Home ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Home details.</returns>
    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetHomeByIdAsync))]
    [ProducesResponseType(typeof(HomeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HomeDto>> GetHomeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var home = await _homeService.GetHomeByIdAsync(id, cancellationToken);
        if (home is null)
        {
            return NotFound();
        }

        return Ok(home);
    }

    /// <summary>
    /// Creates a new home.
    /// </summary>
    /// <param name="request">Create home request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created home.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(HomeOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HomeOperationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HomeOperationResponse>> CreateHomeAsync(
        [FromBody] CreateHomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _homeService.CreateHomeAsync(
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(
            nameof(GetHomeByIdAsync),
            new { id = response.Home!.Id },
            response);
    }

    /// <summary>
    /// Updates a home.
    /// </summary>
    /// <param name="id">Home ID.</param>
    /// <param name="request">Update home request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated home.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(HomeOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HomeOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HomeOperationResponse>> UpdateHomeAsync(
        Guid id,
        [FromBody] UpdateHomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _homeService.UpdateHomeAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Home not found.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Deactivates a home.
    /// </summary>
    /// <param name="id">Home ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deactivated home.</returns>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(HomeOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HomeOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HomeOperationResponse>> DeactivateHomeAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _homeService.DeactivateHomeAsync(
            id,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Home not found.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Reactivates a home.
    /// </summary>
    /// <param name="id">Home ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Reactivated home.</returns>
    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(HomeOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HomeOperationResponse>> ReactivateHomeAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _homeService.ReactivateHomeAsync(
            id,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Home not found.")
            {
                return NotFound();
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    // ========== Bed Management Endpoints ==========

    /// <summary>
    /// Gets all beds for a home.
    /// </summary>
    /// <param name="homeId">Home ID.</param>
    /// <param name="includeInactive">Whether to include inactive beds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of beds.</returns>
    [HttpGet("{homeId:guid}/beds")]
    [ActionName(nameof(GetBedsByHomeIdAsync))]
    [ProducesResponseType(typeof(IReadOnlyList<BedDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BedDto>>> GetBedsByHomeIdAsync(
        Guid homeId,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var beds = await _bedService.GetBedsByHomeIdAsync(homeId, includeInactive, cancellationToken);
        return Ok(beds);
    }

    /// <summary>
    /// Gets available beds for a home.
    /// </summary>
    /// <param name="homeId">Home ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available beds.</returns>
    [HttpGet("{homeId:guid}/beds/available")]
    [ProducesResponseType(typeof(IReadOnlyList<BedDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BedDto>>> GetAvailableBedsAsync(
        Guid homeId,
        CancellationToken cancellationToken = default)
    {
        var beds = await _bedService.GetAvailableBedsAsync(homeId, cancellationToken);
        return Ok(beds);
    }

    /// <summary>
    /// Creates a new bed in a home.
    /// </summary>
    /// <param name="homeId">Home ID.</param>
    /// <param name="request">Create bed request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created bed.</returns>
    [HttpPost("{homeId:guid}/beds")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(BedOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BedOperationResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BedOperationResponse>> CreateBedAsync(
        Guid homeId,
        [FromBody] CreateBedRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _bedService.CreateBedAsync(
            homeId,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(
            nameof(GetBedsByHomeIdAsync),
            new { homeId },
            response);
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
