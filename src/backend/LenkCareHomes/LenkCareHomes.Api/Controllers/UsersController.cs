using System.Security.Claims;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Models.Users;
using LenkCareHomes.Api.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
///     Controller for user management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly IUserService _userService;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    ///     Gets the tour completion status for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tour status.</returns>
    [HttpGet("me/tour-status")]
    [Authorize]
    [ProducesResponseType(typeof(TourStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TourStatusResponse>> GetTourStatusAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var tourCompleted = await _userService.GetTourCompletedAsync(userId.Value, cancellationToken);
        return Ok(new TourStatusResponse { TourCompleted = tourCompleted });
    }

    /// <summary>
    ///     Marks the tour as completed for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message.</returns>
    [HttpPost("me/tour-status/complete")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompleteTourAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        await _userService.SetTourCompletedAsync(userId.Value, true, cancellationToken);
        _logger.LogInformation("User {UserId} completed the onboarding tour", userId.Value);

        return Ok(new { message = "Tour marked as completed." });
    }

    /// <summary>
    ///     Resets the tour status for the current user (allows re-taking the tour).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message.</returns>
    [HttpPost("me/tour-status/reset")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetTourAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        await _userService.SetTourCompletedAsync(userId.Value, false, cancellationToken);
        _logger.LogInformation("User {UserId} reset their onboarding tour status", userId.Value);

        return Ok(new { message = "Tour status reset successfully." });
    }

    /// <summary>
    ///     Gets all users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all users.</returns>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAllUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllUsersAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>
    ///     Gets a user by ID.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User details.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        if (user is null) return NotFound();

        return Ok(user);
    }

    /// <summary>
    ///     Invites a new user to the system.
    /// </summary>
    /// <param name="request">Invitation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Invitation response.</returns>
    [HttpPost("invite")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
    [ProducesResponseType(typeof(InviteUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(InviteUserResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InviteUserResponse>> InviteUserAsync(
        [FromBody] InviteUserRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _userService.InviteUserAsync(
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success) return BadRequest(response);

        return Ok(response);
    }

    /// <summary>
    ///     Resends an invitation email to a user who hasn't accepted yet.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resend invitation response.</returns>
    [HttpPost("{id:guid}/resend-invitation")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
    [ProducesResponseType(typeof(ResendInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResendInvitationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResendInvitationResponse>> ResendInvitationAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var response = await _userService.ResendInvitationAsync(
            id,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            if (response.Error == "User not found.") return NotFound(response);
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    ///     Updates a user's information.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user details.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateUserAsync(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var user = await _userService.UpdateUserAsync(
            id,
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (user is null) return NotFound();

        return Ok(user);
    }

    /// <summary>
    ///     Deactivates a user account.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure.</returns>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var success = await _userService.DeactivateUserAsync(
            id,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!success) return NotFound();

        return Ok(new { message = "User deactivated successfully." });
    }

    /// <summary>
    ///     Permanently deletes a user from the system.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        // Prevent self-deletion
        if (id == currentUserId.Value) return BadRequest(new { error = "You cannot delete your own account." });

        var success = await _userService.DeleteUserAsync(
            id,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!success) return NotFound();

        return Ok(new { message = "User deleted successfully." });
    }

    /// <summary>
    ///     Reactivates a user account.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure.</returns>
    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var success = await _userService.ReactivateUserAsync(
            id,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!success) return NotFound();

        return Ok(new { message = "User reactivated successfully." });
    }

    /// <summary>
    ///     Resets a user's MFA (passkey authentication).
    ///     This removes all passkeys and backup codes, requiring the user to set up new authentication.
    ///     Only Sysadmins can perform this action - requires documented reason and identity verification method.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Reset request with reason and verification details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>MFA reset result.</returns>
    [HttpPost("{id:guid}/reset-mfa")]
    [Authorize(Roles = Roles.Sysadmin)]
    [ProducesResponseType(typeof(MfaResetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetUserMfaAsync(
        Guid id,
        [FromBody] MfaResetRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        // Ensure the request userId matches the route parameter
        if (request.UserId != id)
            return BadRequest(new MfaResetResponse
            {
                Success = false,
                Error = "User ID in request body must match the route parameter."
            });

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new MfaResetResponse
            {
                Success = false,
                Error = "Reason is required for MFA reset."
            });

        if (string.IsNullOrWhiteSpace(request.VerificationMethod))
            return BadRequest(new MfaResetResponse
            {
                Success = false,
                Error = "Verification method is required for MFA reset."
            });

        var result = await _userService.ResetUserMfaAsync(
            request,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!result.Success)
        {
            if (result.Error == "User not found.") return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    ///     Assigns a role to a user.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="role">Role name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure.</returns>
    [HttpPost("{id:guid}/roles/{role}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRoleAsync(Guid id, string role, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var success = await _userService.AssignRoleAsync(
            id,
            role,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!success) return BadRequest(new { error = "Failed to assign role. User not found or invalid role." });

        return Ok(new { message = "Role assigned successfully." });
    }

    /// <summary>
    ///     Removes a role from a user.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="role">Role name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure.</returns>
    [HttpDelete("{id:guid}/roles/{role}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRoleAsync(Guid id, string role, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        var success = await _userService.RemoveRoleAsync(
            id,
            role,
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!success) return NotFound();

        return Ok(new { message = "Role removed successfully." });
    }

    /// <summary>
    ///     Gets available roles.
    /// </summary>
    /// <returns>List of available roles.</returns>
    [HttpGet("roles")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Sysadmin}")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<string>> GetAvailableRoles()
    {
        return Ok(Roles.All);
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