using LenkCareHomes.Api.Models.Auth;
using LenkCareHomes.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// Returns indication of whether passkey authentication or setup is required.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login response indicating success, passkey auth, or passkey setup requirement.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(
            request,
            GetClientIpAddress(),
            GetUserAgent(),
            cancellationToken);

        if (!response.Success && !response.RequiresPasskey && !response.RequiresPasskeySetup)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Verifies a backup code for MFA recovery (Sysadmin only).
    /// </summary>
    /// <param name="request">Backup code verification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification response with auth token on success.</returns>
    [HttpPost("mfa/verify-backup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MfaVerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MfaVerifyResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MfaVerifyResponse>> VerifyBackupCodeAsync(
        [FromBody] BackupCodeVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.VerifyBackupCodeAsync(
            request,
            GetClientIpAddress(),
            GetUserAgent(),
            cancellationToken);

        if (!response.Success)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Initiates passkey-based MFA setup for a user.
    /// Generates backup codes only for Sysadmin users.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="passkeySetupToken">Temporary token for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>MFA setup response with user ID and backup codes (if Sysadmin).</returns>
    [HttpPost("mfa/setup/{userId:guid}")]
    [AllowAnonymous] // Allowed during initial setup flow
    [ProducesResponseType(typeof(MfaSetupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MfaSetupResponse>> SetupMfaAsync(
        Guid userId,
        [FromQuery] string passkeySetupToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.SetupMfaAsync(userId, passkeySetupToken, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid or expired setup token." });
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Confirms MFA setup after the user has registered at least one passkey.
    /// </summary>
    /// <param name="request">MFA setup confirmation request.</param>
    /// <param name="passkeySetupToken">Temporary token for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure.</returns>
    [HttpPost("mfa/setup/confirm")]
    [AllowAnonymous] // Allowed during initial setup flow
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmMfaSetupAsync(
        [FromBody] MfaSetupConfirmRequest request,
        [FromQuery] string passkeySetupToken,
        CancellationToken cancellationToken)
    {
        var success = await _authService.ConfirmMfaSetupAsync(request, passkeySetupToken, cancellationToken);
        if (!success)
        {
            return BadRequest(new { error = "MFA setup confirmation failed. Ensure at least one passkey is registered and token is valid." });
        }

        return Ok(new { message = "MFA setup completed successfully." });
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message.</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is not null)
        {
            await _authService.LogoutAsync(
                userId.Value,
                GetClientIpAddress(),
                GetUserAgent(),
                cancellationToken);
        }

        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Gets the current authenticated user's details.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current user details.</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new CurrentUserResponse
            {
                Success = false,
                Error = "Not authenticated."
            });
        }

        var response = await _authService.GetCurrentUserAsync(userId.Value, cancellationToken);
        if (!response.Success)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Initiates a password reset request.
    /// </summary>
    /// <param name="request">Password reset request with email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success message (always returns success to prevent user enumeration).</returns>
    [HttpPost("password/reset-request")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestPasswordResetAsync(
        [FromBody] PasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.RequestPasswordResetAsync(
            request,
            GetClientIpAddress(),
            cancellationToken);

        // Always return success to prevent user enumeration
        return Ok(new { message = "If an account exists with this email, a password reset link has been sent." });
    }

    /// <summary>
    /// Resets a user's password using a reset token.
    /// </summary>
    /// <param name="request">Password reset confirmation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure.</returns>
    [HttpPost("password/reset")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPasswordAsync(
        [FromBody] PasswordResetConfirmRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _authService.ResetPasswordAsync(
            request,
            GetClientIpAddress(),
            cancellationToken);

        if (!success)
        {
            return BadRequest(new { error = "Invalid or expired reset token, or password does not meet requirements." });
        }

        return Ok(new { message = "Password reset successfully." });
    }

    /// <summary>
    /// Accepts an invitation and sets up the user account.
    /// </summary>
    /// <param name="request">Invitation acceptance request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Invitation acceptance response.</returns>
    [HttpPost("invitation/accept")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InvitationAcceptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(InvitationAcceptResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvitationAcceptResponse>> AcceptInvitationAsync(
        [FromBody] InvitationAcceptRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.AcceptInvitationAsync(
            request,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Updates user profile during account setup.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Profile update request.</param>
    /// <param name="passkeySetupToken">Temporary token for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure.</returns>
    [HttpPut("profile/{userId:guid}")]
    [AllowAnonymous] // Allowed during initial setup flow
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfileAsync(
        Guid userId,
        [FromBody] UpdateProfileRequest request,
        [FromQuery] string passkeySetupToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _authService.UpdateProfileAsync(userId, request, passkeySetupToken, cancellationToken);
            if (!success)
            {
                return BadRequest(new { error = "Failed to update profile. Check if token is valid." });
            }

            return Ok(new { message = "Profile updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Completes the setup process and automatically logs in the user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="passkeySetupToken">Temporary token for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete setup response with authentication token.</returns>
    [HttpPost("complete-setup/{userId:guid}")]
    [AllowAnonymous] // Allowed during initial setup flow
    [ProducesResponseType(typeof(CompleteSetupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CompleteSetupResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompleteSetupResponse>> CompleteSetupAsync(
        Guid userId,
        [FromQuery] string passkeySetupToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.CompleteSetupAsync(
                userId,
                passkeySetupToken,
                GetClientIpAddress(),
                GetUserAgent(),
                cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Regenerates backup codes for the current Sysadmin user.
    /// This invalidates all previous backup codes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response with newly generated backup codes.</returns>
    [HttpPost("backup-codes/regenerate")]
    [Authorize(Roles = "Sysadmin")]
    [ProducesResponseType(typeof(RegenerateBackupCodesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RegenerateBackupCodesResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RegenerateBackupCodesResponse>> RegenerateBackupCodesAsync(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new RegenerateBackupCodesResponse
            {
                Success = false,
                Error = "User not authenticated."
            });
        }

        var response = await _authService.RegenerateBackupCodesAsync(
            userId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    private string? GetClientIpAddress()
    {
        // Check for forwarded headers first (for reverse proxy scenarios)
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return HttpContext.Request.Headers.UserAgent.FirstOrDefault();
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
