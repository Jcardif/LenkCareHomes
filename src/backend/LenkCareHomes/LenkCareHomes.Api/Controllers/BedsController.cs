using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Models.Beds;
using LenkCareHomes.Api.Services.Beds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
/// Controller for bed management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BedsController : ControllerBase
{
    private readonly IBedService _bedService;
    private readonly ILogger<BedsController> _logger;

    public BedsController(
        IBedService bedService,
        ILogger<BedsController> logger)
    {
        _bedService = bedService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a bed by ID.
    /// </summary>
    /// <param name="id">Bed ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bed details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BedDto>> GetBedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var bed = await _bedService.GetBedByIdAsync(id, cancellationToken);
        if (bed is null)
        {
            return NotFound();
        }

        return Ok(bed);
    }

    /// <summary>
    /// Updates a bed.
    /// </summary>
    /// <param name="id">Bed ID.</param>
    /// <param name="request">Update bed request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated bed.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(BedOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BedOperationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BedOperationResponse>> UpdateBedAsync(
        Guid id,
        [FromBody] UpdateBedRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var response = await _bedService.UpdateBedAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "Bed not found.")
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
