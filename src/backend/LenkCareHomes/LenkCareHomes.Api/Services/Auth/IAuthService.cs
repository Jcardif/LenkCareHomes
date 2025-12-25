using LenkCareHomes.Api.Models.Auth;

namespace LenkCareHomes.Api.Services.Auth;

/// <summary>
///     Service interface for authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    ///     Authenticates a user with email and password (step 1 of passkey authentication).
    ///     If successful, returns a response indicating passkey authentication is required.
    /// </summary>
    /// <param name="request">Login request with credentials.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="userAgent">Client user agent for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login response indicating next step (passkey auth, passkey setup, or error).</returns>
    Task<LoginResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Verifies a backup code for MFA recovery (Sysadmin only).
    /// </summary>
    /// <param name="request">Backup code verification request.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="userAgent">Client user agent for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>MFA verification response.</returns>
    Task<MfaVerifyResponse> VerifyBackupCodeAsync(
        BackupCodeVerifyRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Initiates passkey-based MFA setup for a user during account setup.
    ///     Generates backup codes only for Sysadmin users.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="passkeySetupToken">Temporary token for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>MFA setup response with user ID and backup codes (if Sysadmin).</returns>
    Task<MfaSetupResponse> SetupMfaAsync(Guid userId, string passkeySetupToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Confirms MFA setup after the user has registered at least one passkey.
    /// </summary>
    /// <param name="request">MFA setup confirmation request.</param>
    /// <param name="passkeySetupToken">Temporary token for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if setup is confirmed successfully.</returns>
    Task<bool> ConfirmMfaSetupAsync(MfaSetupConfirmRequest request, string passkeySetupToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Logs out the current user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="userAgent">Client user agent for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogoutAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Initiates a password reset request.
    /// </summary>
    /// <param name="request">Password reset request with email.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RequestPasswordResetAsync(
        PasswordResetRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets a user's password using a reset token.
    /// </summary>
    /// <param name="request">Password reset confirmation request.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if password was reset successfully.</returns>
    Task<bool> ResetPasswordAsync(
        PasswordResetConfirmRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Accepts an invitation and sets up the user account.
    /// </summary>
    /// <param name="request">Invitation acceptance request.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response with passkey setup information.</returns>
    Task<InvitationAcceptResponse> AcceptInvitationAsync(
        InvitationAcceptRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates user profile during account setup.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Profile update request.</param>
    /// <param name="passkeySetupToken">Temporary token for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if profile was updated successfully.</returns>
    Task<bool> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        string passkeySetupToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Completes the setup process and automatically logs in the user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="passkeySetupToken">Temporary token for validation.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="userAgent">Client user agent for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete setup response with authentication token.</returns>
    Task<CompleteSetupResponse> CompleteSetupAsync(
        Guid userId,
        string passkeySetupToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the currently authenticated user's details.
    /// </summary>
    /// <param name="userId">User ID from the authentication context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current user response with user details.</returns>
    Task<CurrentUserResponse> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Regenerates backup codes for a Sysadmin user.
    ///     This invalidates all previous backup codes.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response with newly generated backup codes.</returns>
    Task<RegenerateBackupCodesResponse> RegenerateBackupCodesAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}