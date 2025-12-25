namespace LenkCareHomes.Api.Models.Users;

/// <summary>
/// DTO for user information.
/// </summary>
public sealed record UserDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}";
    public required bool IsActive { get; init; }
    public required bool IsMfaSetupComplete { get; init; }
    public required bool InvitationAccepted { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Request model for inviting a new user.
/// </summary>
public sealed record InviteUserRequest
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// Gets or sets the role to assign to the user.
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// Gets or sets the home IDs to assign to the user (for Caregiver role only).
    /// </summary>
    public IReadOnlyList<Guid>? HomeIds { get; init; }
}

/// <summary>
/// Response model for user invitation.
/// </summary>
public sealed record InviteUserResponse
{
    public bool Success { get; init; }
    public Guid? UserId { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Request model for updating a user.
/// </summary>
public sealed record UpdateUserRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
}

/// <summary>
/// Response model for tour status.
/// </summary>
public sealed record TourStatusResponse
{
    /// <summary>
    /// Gets or sets whether the user has completed the onboarding tour.
    /// </summary>
    public bool TourCompleted { get; init; }
}

/// <summary>
/// Request model for resetting a user's MFA (passkeys and backup codes).
/// </summary>
public sealed record MfaResetRequest
{
    /// <summary>
    /// Gets or sets the ID of the user whose MFA is being reset.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets the documented reason for the MFA reset.
    /// This is required for HIPAA audit compliance.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets or sets the method used to verify the user's identity before reset.
    /// Examples: "in-person verification", "video call verification", "phone verification with security questions"
    /// </summary>
    public required string VerificationMethod { get; init; }

    /// <summary>
    /// Gets or sets any additional notes about the reset request.
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Response model for MFA reset operation.
/// </summary>
public sealed record MfaResetResponse
{
    /// <summary>
    /// Gets or sets whether the reset was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the number of passkeys that were removed.
    /// </summary>
    public int PasskeysRemoved { get; init; }

    /// <summary>
    /// Gets or sets any error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets or sets a message describing the result.
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Response model for resending an invitation.
/// </summary>
public sealed record ResendInvitationResponse
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets any error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets or sets a message describing the result.
    /// </summary>
    public string? Message { get; init; }
}
