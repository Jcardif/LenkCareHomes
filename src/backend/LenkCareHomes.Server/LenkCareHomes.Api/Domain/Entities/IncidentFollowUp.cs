namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents a follow-up note on an incident report.
/// </summary>
public sealed class IncidentFollowUp
{
    /// <summary>
    ///     Gets or sets the unique identifier for the follow-up.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the incident this follow-up belongs to.
    /// </summary>
    public Guid IncidentId { get; set; }

    /// <summary>
    ///     Gets or sets the user who created the follow-up note.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    ///     Gets or sets the follow-up note text.
    /// </summary>
    public required string Note { get; set; }

    /// <summary>
    ///     Gets or sets when the follow-up was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Navigation property for the incident.
    /// </summary>
    public Incident? Incident { get; set; }

    /// <summary>
    ///     Navigation property for the user who created the follow-up.
    /// </summary>
    public ApplicationUser? CreatedBy { get; set; }
}