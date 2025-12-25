using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
/// Represents a recreational or other activity.
/// Can be individual or group activity.
/// </summary>
public sealed class Activity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the activity name.
    /// </summary>
    public required string ActivityName { get; set; }

    /// <summary>
    /// Gets or sets the activity description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the date of the activity.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the duration in minutes.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// Gets or sets the activity category.
    /// </summary>
    public ActivityCategory Category { get; set; }

    /// <summary>
    /// Gets or sets whether this is a group activity.
    /// </summary>
    public bool IsGroupActivity { get; set; }

    /// <summary>
    /// Gets or sets the home ID where the activity took place (for group activities).
    /// </summary>
    public Guid? HomeId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who created this activity.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Gets or sets when the record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property for the home.
    /// </summary>
    public Home? Home { get; set; }

    /// <summary>
    /// Navigation property for the creator.
    /// </summary>
    public ApplicationUser? CreatedBy { get; set; }

    /// <summary>
    /// Navigation property for participants.
    /// </summary>
    public ICollection<ActivityParticipant> Participants { get; set; } = new List<ActivityParticipant>();
}
