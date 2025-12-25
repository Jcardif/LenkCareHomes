using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
/// Represents a behavior or mood note entry.
/// Contains PHI that must be protected under HIPAA.
/// </summary>
public sealed class BehaviorNote
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the client ID.
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Gets or sets the caregiver ID who recorded this entry.
    /// </summary>
    public Guid CaregiverId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the observation was made.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the category (Behavior, Mood, or General).
    /// </summary>
    public BehaviorCategory Category { get; set; }

    /// <summary>
    /// Gets or sets the note text content.
    /// </summary>
    public required string NoteText { get; set; }

    /// <summary>
    /// Gets or sets the severity level (optional).
    /// </summary>
    public NoteSeverity? Severity { get; set; }

    /// <summary>
    /// Gets or sets when the record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the client.
    /// </summary>
    public Client? Client { get; set; }

    /// <summary>
    /// Navigation property for the caregiver.
    /// </summary>
    public ApplicationUser? Caregiver { get; set; }
}
