namespace LenkCareHomes.Api.Models.Passkey;

/// <summary>
/// Request to begin passkey registration.
/// </summary>
public sealed record PasskeyRegistrationBeginRequest
{
    /// <summary>
    /// Gets or sets the user-friendly name for this device/passkey.
    /// </summary>
    public required string DeviceName { get; init; }
}

/// <summary>
/// Response containing the WebAuthn credential creation options.
/// </summary>
public sealed record PasskeyRegistrationBeginResponse
{
    /// <summary>
    /// Gets or sets whether the request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the session ID for completing registration.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets or sets the credential creation options (JSON serialized).
    /// This is passed directly to navigator.credentials.create().
    /// </summary>
    public string? Options { get; init; }

    /// <summary>
    /// Gets or sets an error message if the request failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Request to complete passkey registration.
/// </summary>
public sealed record PasskeyRegistrationCompleteRequest
{
    /// <summary>
    /// Gets or sets the session ID from the begin response.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets or sets the attestation response from the authenticator (JSON serialized).
    /// This is the result from navigator.credentials.create().
    /// </summary>
    public required string AttestationResponse { get; init; }

    /// <summary>
    /// Gets or sets the device name for this passkey.
    /// </summary>
    public required string DeviceName { get; init; }
}

/// <summary>
/// Response for passkey registration completion.
/// </summary>
public sealed record PasskeyRegistrationCompleteResponse
{
    /// <summary>
    /// Gets or sets whether the registration was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the ID of the newly registered passkey.
    /// </summary>
    public Guid? PasskeyId { get; init; }

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string? DeviceName { get; init; }

    /// <summary>
    /// Gets or sets the total number of passkeys the user now has.
    /// </summary>
    public int? TotalPasskeys { get; init; }

    /// <summary>
    /// Gets or sets an error message if registration failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Request to begin passkey authentication.
/// </summary>
public sealed record PasskeyAuthenticationBeginRequest
{
    /// <summary>
    /// Gets or sets the email of the user attempting to authenticate (optional).
    /// If not provided, authenticates based on resident credentials.
    /// </summary>
    public string? Email { get; init; }
}

/// <summary>
/// Response containing the WebAuthn assertion options.
/// </summary>
public sealed record PasskeyAuthenticationBeginResponse
{
    /// <summary>
    /// Gets or sets whether the request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the session ID for completing authentication.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets or sets the assertion options (JSON serialized).
    /// This is passed directly to navigator.credentials.get().
    /// </summary>
    public string? Options { get; init; }

    /// <summary>
    /// Gets or sets an error message if the request failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Request to complete passkey authentication.
/// </summary>
public sealed record PasskeyAuthenticationCompleteRequest
{
    /// <summary>
    /// Gets or sets the session ID from the begin response.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets or sets the assertion response from the authenticator (JSON serialized).
    /// This is the result from navigator.credentials.get().
    /// </summary>
    public required string AssertionResponse { get; init; }
}

/// <summary>
/// Response for passkey authentication completion.
/// </summary>
public sealed record PasskeyAuthenticationCompleteResponse
{
    /// <summary>
    /// Gets or sets whether the authentication was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Gets or sets the authentication token.
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    /// Gets or sets the user's roles.
    /// </summary>
    public IReadOnlyList<string>? Roles { get; init; }

    /// <summary>
    /// Gets or sets the device name of the passkey used.
    /// </summary>
    public string? DeviceName { get; init; }

    /// <summary>
    /// Gets or sets an error message if authentication failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Represents a user's registered passkey for display purposes.
/// </summary>
public sealed record PasskeyDto
{
    /// <summary>
    /// Gets or sets the passkey ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the user-friendly device name.
    /// </summary>
    public required string DeviceName { get; init; }

    /// <summary>
    /// Gets or sets when this passkey was registered.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets when this passkey was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; init; }

    /// <summary>
    /// Gets or sets whether this passkey is currently active.
    /// </summary>
    public bool IsActive { get; init; }
}

/// <summary>
/// Response containing the user's registered passkeys.
/// </summary>
public sealed record PasskeyListResponse
{
    /// <summary>
    /// Gets or sets the list of passkeys.
    /// </summary>
    public required IReadOnlyList<PasskeyDto> Passkeys { get; init; }

    /// <summary>
    /// Gets or sets the total count.
    /// </summary>
    public int TotalCount { get; init; }
}

/// <summary>
/// Request to update a passkey's device name.
/// </summary>
public sealed record UpdatePasskeyRequest
{
    /// <summary>
    /// Gets or sets the new device name.
    /// </summary>
    public required string DeviceName { get; init; }
}

/// <summary>
/// Response for passkey deletion.
/// </summary>
public sealed record DeletePasskeyResponse
{
    /// <summary>
    /// Gets or sets whether the deletion was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the remaining passkey count.
    /// </summary>
    public int RemainingPasskeys { get; init; }

    /// <summary>
    /// Gets or sets an error message if deletion failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Request for MFA reset by a Sysadmin.
/// </summary>
public sealed record MfaResetRequest
{
    /// <summary>
    /// Gets or sets the reason for the MFA reset.
    /// Required for audit logging.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets or sets the verification method used to confirm user identity.
    /// Examples: "phone_call", "in_person", "manager_confirmation".
    /// </summary>
    public required string VerificationMethod { get; init; }
}

/// <summary>
/// Response for MFA reset.
/// </summary>
public sealed record MfaResetResponse
{
    /// <summary>
    /// Gets or sets whether the reset was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets an error message if the reset failed.
    /// </summary>
    public string? Error { get; init; }
}
