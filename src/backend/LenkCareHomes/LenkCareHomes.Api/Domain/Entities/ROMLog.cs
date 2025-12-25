namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
/// Represents a Range of Motion (ROM) exercise log entry.
/// Contains PHI that must be protected under HIPAA.
/// </summary>
public sealed class ROMLog
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
    /// Gets or sets the timestamp when the exercise was performed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the activity description (e.g., "Shoulder ROM", "Leg ROM").
    /// </summary>
    public required string ActivityDescription { get; set; }

    /// <summary>
    /// Gets or sets the duration in minutes.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// Gets or sets the number of repetitions.
    /// </summary>
    public int? Repetitions { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the client's response.
    /// </summary>
    public string? Notes { get; set; }

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
