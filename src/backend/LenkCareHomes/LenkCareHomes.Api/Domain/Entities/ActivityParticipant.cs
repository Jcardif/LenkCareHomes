namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents a client's participation in an activity.
///     Junction table for Activity and Client many-to-many relationship.
/// </summary>
public sealed class ActivityParticipant
{
    /// <summary>
    ///     Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the activity ID.
    /// </summary>
    public Guid ActivityId { get; set; }

    /// <summary>
    ///     Gets or sets the client ID (participant).
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    ///     Gets or sets when this participation record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Navigation property for the activity.
    /// </summary>
    public Activity? Activity { get; set; }

    /// <summary>
    ///     Navigation property for the client.
    /// </summary>
    public Client? Client { get; set; }
}