namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents a WebAuthn/FIDO2 passkey credential registered by a user.
///     Supports multiple passkeys per user for device redundancy.
/// </summary>
public sealed class UserPasskey
{
    /// <summary>
    ///     Gets or sets the unique identifier for this passkey.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the user ID who owns this passkey.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     Gets or sets the WebAuthn credential ID (base64 encoded).
    ///     This is used to identify the credential during authentication.
    /// </summary>
    public required string CredentialId { get; set; }

    /// <summary>
    ///     Gets or sets the public key (COSE format, base64 encoded).
    ///     Used to verify authentication assertions.
    /// </summary>
    public required string PublicKey { get; set; }

    /// <summary>
    ///     Gets or sets the signature counter for replay attack protection.
    ///     Should be incremented on each successful authentication.
    /// </summary>
    public uint SignatureCounter { get; set; }

    /// <summary>
    ///     Gets or sets the AAGUID of the authenticator (if available).
    ///     Helps identify the type of authenticator used.
    /// </summary>
    public string? AaGuid { get; set; }

    /// <summary>
    ///     Gets or sets the user-friendly device name.
    ///     Examples: "iPhone 15", "Work Laptop", "Windows Hello".
    /// </summary>
    public required string DeviceName { get; set; }

    /// <summary>
    ///     Gets or sets the credential type (e.g., "public-key").
    /// </summary>
    public string CredentialType { get; set; } = "public-key";

    /// <summary>
    ///     Gets or sets the transports supported by this credential.
    ///     Stored as comma-separated values (e.g., "internal,hybrid,usb").
    /// </summary>
    public string? Transports { get; set; }

    /// <summary>
    ///     Gets or sets when this passkey was registered.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets when this passkey was last used for authentication.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    ///     Gets or sets whether this passkey is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Navigation property to the user who owns this passkey.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;
}