using Microsoft.AspNetCore.Identity;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
/// Represents an application user with extended identity properties.
/// Supports Admin, Caregiver, and Developer/Sysadmin roles.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Gets or sets whether the user has completed MFA setup (has at least one passkey registered).
    /// </summary>
    public bool IsMfaSetupComplete { get; set; }

    /// <summary>
    /// Gets or sets whether the user's MFA has been reset and requires passkey setup.
    /// When true, the user must register a new passkey before accessing the application.
    /// </summary>
    public bool RequiresPasskeySetup { get; set; }

    /// <summary>
    /// Gets or sets the encrypted backup codes for MFA recovery.
    /// Stored as JSON array of hashed codes.
    /// Note: Backup codes are only available to Sysadmin users as a self-service recovery option.
    /// </summary>
    public string? BackupCodesHash { get; set; }

    /// <summary>
    /// Gets or sets the number of remaining backup codes.
    /// </summary>
    public int RemainingBackupCodes { get; set; }

    /// <summary>
    /// Gets or sets when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the user was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the invitation token for initial account setup.
    /// </summary>
    public string? InvitationToken { get; set; }

    /// <summary>
    /// Gets or sets when the invitation expires.
    /// </summary>
    public DateTime? InvitationExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether the invitation has been accepted.
    /// </summary>
    public bool InvitationAccepted { get; set; }

    /// <summary>
    /// Gets or sets whether the user has completed the onboarding tour.
    /// Used to determine if the guided tour should be shown on login.
    /// </summary>
    public bool TourCompleted { get; set; }

    /// <summary>
    /// Gets the user's full name.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Navigation property for caregiver home assignments.
    /// </summary>
    public ICollection<CaregiverHomeAssignment> HomeAssignments { get; set; } = [];

    /// <summary>
    /// Navigation property for registered passkeys.
    /// </summary>
    public ICollection<UserPasskey> Passkeys { get; set; } = [];
}
