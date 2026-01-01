using System.Security.Claims;
using LenkCareHomes.Api.Models.Passkey;
using LenkCareHomes.Api.Services.Passkey;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
///     Controller for WebAuthn/FIDO2 passkey operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PasskeyController : ControllerBase
{
    private readonly ILogger<PasskeyController> _logger;
    private readonly IPasskeyService _passkeyService;

    public PasskeyController(
        IPasskeyService passkeyService,
        ILogger<PasskeyController> logger)
    {
        _passkeyService = passkeyService;
        _logger = logger;
    }

    /// <summary>
    ///     Begins passkey registration for the current user or a specified user during setup.
    /// </summary>
    /// <param name="userId">User ID (optional, uses current user if not provided).</param>
    /// <param name="passkeySetupToken">Temporary token for setup flow (required if userId is provided).</param>
    /// <param name="request">Registration begin request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>WebAuthn credential creation options.</returns>
    [HttpPost("register/begin")]
    [AllowAnonymous] // Allowed during initial setup flow
    [ProducesResponseType(typeof(PasskeyRegistrationBeginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PasskeyRegistrationBeginResponse>> BeginRegistrationAsync(
        [FromQuery] Guid? userId,
        [FromQuery] string? passkeySetupToken,
        [FromBody] PasskeyRegistrationBeginRequest request,
        CancellationToken cancellationToken)
    {
        Guid targetUserId;

        if (userId.HasValue)
        {
            // Setup flow: MUST have passkeySetupToken
            if (string.IsNullOrEmpty(passkeySetupToken))
                return Unauthorized(new { error = "Temporary token is required for setup flow." });
            targetUserId = userId.Value;
        }
        else
        {
            // Authenticated flow: MUST have currentUserId
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            targetUserId = currentUserId.Value;
        }

        var response = await _passkeyService.BeginRegistrationAsync(
            targetUserId,
            request.DeviceName,
            passkeySetupToken,
            cancellationToken);

        if (!response.Success) return BadRequest(response);

        return Ok(response);
    }

    /// <summary>
    ///     Completes passkey registration.
    /// </summary>
    /// <param name="userId">User ID (optional, uses current user if not provided).</param>
    /// <param name="passkeySetupToken">Temporary token for setup flow (required if userId is provided).</param>
    /// <param name="request">Registration complete request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration result.</returns>
    [HttpPost("register/complete")]
    [AllowAnonymous] // Allowed during initial setup flow
    [ProducesResponseType(typeof(PasskeyRegistrationCompleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PasskeyRegistrationCompleteResponse>> CompleteRegistrationAsync(
        [FromQuery] Guid? userId,
        [FromQuery] string? passkeySetupToken,
        [FromBody] PasskeyRegistrationCompleteRequest request,
        CancellationToken cancellationToken)
    {
        Guid targetUserId;

        if (userId.HasValue)
        {
            // Setup flow: MUST have passkeySetupToken
            if (string.IsNullOrEmpty(passkeySetupToken))
                return Unauthorized(new { error = "Temporary token is required for setup flow." });
            targetUserId = userId.Value;
        }
        else
        {
            // Authenticated flow: MUST have currentUserId
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            targetUserId = currentUserId.Value;
        }

        var response = await _passkeyService.CompleteRegistrationAsync(
            targetUserId,
            request,
            GetClientIpAddress(),
            GetUserAgent(),
            passkeySetupToken,
            cancellationToken);

        if (!response.Success) return BadRequest(response);

        return Ok(response);
    }

    /// <summary>
    ///     Begins passkey authentication.
    /// </summary>
    /// <param name="request">Authentication begin request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>WebAuthn assertion options.</returns>
    [HttpPost("authenticate/begin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasskeyAuthenticationBeginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PasskeyAuthenticationBeginResponse>> BeginAuthenticationAsync(
        [FromBody] PasskeyAuthenticationBeginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _passkeyService.BeginAuthenticationAsync(
            request.Email,
            cancellationToken);

        if (!response.Success) return BadRequest(response);

        return Ok(response);
    }

    /// <summary>
    ///     Completes passkey authentication.
    /// </summary>
    /// <param name="request">Authentication complete request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result with token.</returns>
    [HttpPost("authenticate/complete")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasskeyAuthenticationCompleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PasskeyAuthenticationCompleteResponse>> CompleteAuthenticationAsync(
        [FromBody] PasskeyAuthenticationCompleteRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _passkeyService.CompleteAuthenticationAsync(
            request,
            GetClientIpAddress(),
            GetUserAgent(),
            cancellationToken);

        if (!response.Success) return Unauthorized(response);

        return Ok(response);
    }

    /// <summary>
    ///     Gets all passkeys for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of passkeys.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PasskeyListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PasskeyListResponse>> GetPasskeysAsync(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var response = await _passkeyService.GetUserPasskeysAsync(userId.Value, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    ///     Updates a passkey's device name.
    /// </summary>
    /// <param name="passkeyId">Passkey ID.</param>
    /// <param name="request">Update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or failure.</returns>
    [HttpPut("{passkeyId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePasskeyAsync(
        Guid passkeyId,
        [FromBody] UpdatePasskeyRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var success = await _passkeyService.UpdatePasskeyAsync(
            userId.Value,
            passkeyId,
            request,
            GetClientIpAddress(),
            cancellationToken);

        if (!success) return NotFound(new { error = "Passkey not found." });

        return Ok(new { message = "Passkey updated successfully." });
    }

    /// <summary>
    ///     Deletes a passkey.
    /// </summary>
    /// <param name="passkeyId">Passkey ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deletion result.</returns>
    [HttpDelete("{passkeyId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(DeletePasskeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DeletePasskeyResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DeletePasskeyResponse>> DeletePasskeyAsync(
        Guid passkeyId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var response = await _passkeyService.DeletePasskeyAsync(
            userId.Value,
            passkeyId,
            GetClientIpAddress(),
            cancellationToken);

        if (!response.Success) return BadRequest(response);

        return Ok(response);
    }

    /// <summary>
    ///     Gets the passkey count for a user (used during setup).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Passkey count.</returns>
    [HttpGet("count/{userId:guid}")]
    [AllowAnonymous] // Allowed during setup flow
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetPasskeyCountAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var count = await _passkeyService.GetPasskeyCountAsync(userId, cancellationToken);
        return Ok(new { count });
    }

    private string? GetClientIpAddress()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor)) return forwardedFor.Split(',').First().Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return HttpContext.Request.Headers.UserAgent.FirstOrDefault();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId)) return userId;
        return null;
    }
}