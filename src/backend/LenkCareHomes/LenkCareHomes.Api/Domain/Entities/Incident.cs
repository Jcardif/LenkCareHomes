using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents an incident report for a client.
///     Contains PHI that must be protected under HIPAA.
///     Incidents are retained indefinitely (no deletion).
/// </summary>
public sealed class Incident
{
    /// <summary>
    ///     Gets or sets the unique identifier for the incident.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the auto-generated incident number (e.g., INC-20241129-001).
    /// </summary>
    public required string IncidentNumber { get; set; }

    /// <summary>
    ///     Gets or sets the client involved in the incident (optional for home-level incidents).
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>
    ///     Gets or sets the home where the incident occurred.
    /// </summary>
    public Guid HomeId { get; set; }

    /// <summary>
    ///     Gets or sets the user who reported the incident.
    /// </summary>
    public Guid ReportedById { get; set; }

    /// <summary>
    ///     Gets or sets the type of incident.
    /// </summary>
    public IncidentType IncidentType { get; set; }

    /// <summary>
    ///     Gets or sets the severity level (1-5, where 5 is most severe).
    /// </summary>
    public int Severity { get; set; } = 3;

    /// <summary>
    ///     Gets or sets when the incident occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    ///     Gets or sets the location where the incident occurred.
    /// </summary>
    public required string Location { get; set; }

    /// <summary>
    ///     Gets or sets the description of the incident.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    ///     Gets or sets the actions taken in response to the incident.
    /// </summary>
    public string? ActionsTaken { get; set; }

    /// <summary>
    ///     Gets or sets the names of witnesses to the incident.
    /// </summary>
    public string? WitnessNames { get; set; }

    /// <summary>
    ///     Gets or sets who was notified about the incident.
    /// </summary>
    public string? NotifiedParties { get; set; }

    /// <summary>
    ///     Gets or sets when administrators were notified.
    /// </summary>
    public DateTime? AdminNotifiedAt { get; set; }

    /// <summary>
    ///     Gets or sets the current status of the incident report.
    /// </summary>
    public IncidentStatus Status { get; set; } = IncidentStatus.Draft;

    /// <summary>
    ///     Gets or sets the user who closed the incident (Admin only).
    /// </summary>
    public Guid? ClosedById { get; set; }

    /// <summary>
    ///     Gets or sets when the incident was closed.
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    ///     Gets or sets the closure notes.
    /// </summary>
    public string? ClosureNotes { get; set; }

    /// <summary>
    ///     Gets or sets when the incident was reported.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets when the incident was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     Navigation property for the client.
    /// </summary>
    public Client? Client { get; set; }

    /// <summary>
    ///     Navigation property for the home.
    /// </summary>
    public Home? Home { get; set; }

    /// <summary>
    ///     Navigation property for the reporting user.
    /// </summary>
    public ApplicationUser? ReportedBy { get; set; }

    /// <summary>
    ///     Navigation property for the user who closed the incident.
    /// </summary>
    public ApplicationUser? ClosedBy { get; set; }

    /// <summary>
    ///     Navigation property for follow-up notes.
    /// </summary>
    public ICollection<IncidentFollowUp> FollowUps { get; set; } = [];

    /// <summary>
    ///     Navigation property for attached photos.
    /// </summary>
    public ICollection<IncidentPhoto> Photos { get; set; } = [];
}