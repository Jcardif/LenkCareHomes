using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Models.Dashboard;
using LenkCareHomes.Api.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
/// Controller for dashboard statistics operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Gets dashboard statistics for admin users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Admin dashboard statistics.</returns>
    [HttpGet("admin")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(AdminDashboardStats), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminDashboardStats>> GetAdminDashboardStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var stats = await _dashboardService.GetAdminDashboardStatsAsync(cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Gets dashboard statistics for caregiver users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Caregiver dashboard statistics.</returns>
    [HttpGet("caregiver")]
    [Authorize(Roles = Roles.Caregiver)]
    [ProducesResponseType(typeof(CaregiverDashboardStats), StatusCodes.Status200OK)]
    public async Task<ActionResult<CaregiverDashboardStats>> GetCaregiverDashboardStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var stats = await _dashboardService.GetCaregiverDashboardStatsAsync(currentUserId.Value, cancellationToken);
        return Ok(stats);
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
