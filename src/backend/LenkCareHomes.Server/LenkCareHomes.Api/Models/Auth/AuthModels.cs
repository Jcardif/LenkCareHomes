namespace LenkCareHomes.Api.Models.Auth;

/// <summary>
///     Request model for user login.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    ///     Gets or sets the user's email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    ///     Gets or sets the user's password.
    /// </summary>
    public required string Password { get; init; }
}

/// <summary>
///     Response model for login attempt.
/// </summary>
public sealed record LoginResponse
{
    /// <summary>
    ///     Gets or sets whether the login was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Gets or sets whether passkey authentication is required.
    ///     When true, the client should redirect to passkey authentication.
    /// </summary>
    public bool RequiresPasskey { get; init; }

    /// <summary>
    ///     Gets or sets whether passkey setup is required (first login or after MFA reset).
    ///     When true, the client should redirect to passkey registration.
    /// </summary>
    public bool RequiresPasskeySetup { get; init; }

    /// <summary>
    ///     Gets or sets the user's email (for passkey authentication flow).
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    ///     Gets or sets the user ID (for passkey setup or after successful full authentication).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    ///     Gets or sets a temporary token for MFA setup (only set when RequiresPasskeySetup is true).
    /// </summary>
    public string? PasskeySetupToken { get; init; }

    /// <summary>
    ///     Gets or sets the authentication token (only set after successful full authentication).
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    ///     Gets or sets the user's roles.
    /// </summary>
    public IReadOnlyList<string>? Roles { get; init; }

    /// <summary>
    ///     Gets or sets whether the user is a Sysadmin (has backup codes for recovery).
    /// </summary>
    public bool IsSysadmin { get; init; }

    /// <summary>
    ///     Gets or sets an error message if login failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
///     Request model for backup code verification.
///     Backup codes are only available for Sysadmin accounts.
/// </summary>
public sealed record BackupCodeVerifyRequest
{
    /// <summary>
    ///     Gets or sets the user ID for backup code verification.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Gets or sets the backup code.
    /// </summary>
    public required string BackupCode { get; init; }
}

/// <summary>
///     Response model for MFA verification.
/// </summary>
public sealed record MfaVerifyResponse
{
    /// <summary>
    ///     Gets or sets whether the verification was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Gets or sets the user ID.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    ///     Gets or sets the authentication token.
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    ///     Gets or sets the user's roles.
    /// </summary>
    public IReadOnlyList<string>? Roles { get; init; }

    /// <summary>
    ///     Gets or sets remaining backup codes (only shown when using backup code).
    /// </summary>
    public int? RemainingBackupCodes { get; init; }

    /// <summary>
    ///     Gets or sets whether the user needs to set up a new passkey.
    /// </summary>
    public bool RequiresPasskeySetup { get; init; }

    /// <summary>
    ///     Gets or sets the passkey setup token (when RequiresPasskeySetup is true).
    /// </summary>
    public string? PasskeySetupToken { get; init; }

    /// <summary>
    ///     Gets or sets an error message if verification failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
///     Response model for MFA setup (passkey-based).
/// </summary>
public sealed record MfaSetupResponse
{
    /// <summary>
    ///     Gets or sets the user ID for passkey registration.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Gets or sets the user's email for display.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    ///     Gets or sets the backup codes (only provided for Sysadmin role).
    /// </summary>
    public IReadOnlyList<string>? BackupCodes { get; init; }

    /// <summary>
    ///     Gets or sets whether backup codes are available (Sysadmin only).
    /// </summary>
    public bool HasBackupCodes { get; init; }

    /// <summary>
    ///     Gets or sets whether the user has already completed their profile setup.
    ///     True if the user has a first and last name set.
    /// </summary>
    public bool HasProfileCompleted { get; init; }
}

/// <summary>
///     Request model for confirming passkey-based MFA setup.
/// </summary>
public sealed record MfaSetupConfirmRequest
{
    /// <summary>
    ///     Gets or sets the user ID.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Gets or sets whether the user has confirmed they've saved their backup codes (Sysadmin only).
    /// </summary>
    public bool BackupCodesSaved { get; init; }
}

/// <summary>
///     Request model for password reset.
/// </summary>
public sealed record PasswordResetRequest
{
    /// <summary>
    ///     Gets or sets the user's email address.
    /// </summary>
    public required string Email { get; init; }
}

/// <summary>
///     Request model for confirming password reset.
/// </summary>
public sealed record PasswordResetConfirmRequest
{
    /// <summary>
    ///     Gets or sets the password reset token.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    ///     Gets or sets the new password.
    /// </summary>
    public required string NewPassword { get; init; }
}

/// <summary>
///     Request model for accepting an invitation.
/// </summary>
public sealed record InvitationAcceptRequest
{
    /// <summary>
    ///     Gets or sets the invitation token.
    /// </summary>
    public required string InvitationToken { get; init; }

    /// <summary>
    ///     Gets or sets the new password.
    /// </summary>
    public required string Password { get; init; }
}

/// <summary>
///     Response model for invitation acceptance.
/// </summary>
public sealed record InvitationAcceptResponse
{
    /// <summary>
    ///     Gets or sets whether the invitation was accepted successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Gets or sets the user ID.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    ///     Gets or sets the user's first name (from invitation).
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    ///     Gets or sets the user's last name (from invitation).
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    ///     Gets or sets the MFA setup information.
    /// </summary>
    public MfaSetupResponse? MfaSetup { get; init; }

    /// <summary>
    ///     Gets or sets a temporary token for completing setup.
    /// </summary>
    public string? PasskeySetupToken { get; init; }

    /// <summary>
    ///     Gets or sets an error message if acceptance failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
///     Request model for updating user profile during setup.
/// </summary>
public sealed record UpdateProfileRequest
{
    /// <summary>
    ///     Gets or sets the user's first name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    ///     Gets or sets the user's last name.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    ///     Gets or sets the user's phone number (optional).
    /// </summary>
    public string? PhoneNumber { get; init; }
}

/// <summary>
///     Response model for completing setup and auto-login.
/// </summary>
public sealed record CompleteSetupResponse
{
    /// <summary>
    ///     Gets or sets whether the setup completion was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Gets or sets the user ID.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    ///     Gets or sets the authentication token.
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    ///     Gets or sets the user's roles.
    /// </summary>
    public IReadOnlyList<string>? Roles { get; init; }

    /// <summary>
    ///     Gets or sets the user details.
    /// </summary>
    public UserDto? User { get; init; }

    /// <summary>
    ///     Gets or sets an error message if completion failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
///     User DTO for complete setup response.
/// </summary>
public sealed record UserDto
{
    /// <summary>
    ///     Gets or sets the user ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     Gets or sets the user's email.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    ///     Gets or sets the user's first name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    ///     Gets or sets the user's last name.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    ///     Gets or sets the user's full name.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    ///     Gets or sets the user's roles.
    /// </summary>
    public required IReadOnlyList<string> Roles { get; init; }

    /// <summary>
    ///     Gets or sets whether the user has completed the onboarding tour.
    /// </summary>
    public bool TourCompleted { get; init; }
}

/// <summary>
///     Response model for getting current user.
/// </summary>
public sealed record CurrentUserResponse
{
    /// <summary>
    ///     Gets or sets whether the request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Gets or sets the current user.
    /// </summary>
    public UserDto? User { get; init; }

    /// <summary>
    ///     Gets or sets an error message if the request failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
///     Response model for regenerating backup codes.
/// </summary>
public sealed record RegenerateBackupCodesResponse
{
    /// <summary>
    ///     Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Gets or sets the newly generated backup codes.
    ///     These are shown only once and should be saved by the user.
    /// </summary>
    public IReadOnlyList<string>? BackupCodes { get; init; }

    /// <summary>
    ///     Gets or sets an error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}