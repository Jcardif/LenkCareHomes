namespace LenkCareHomes.Api.Domain.Enums;

/// <summary>
///     Status values for incident reports workflow.
/// </summary>
public enum IncidentStatus
{
    /// <summary>
    ///     Incident report is being drafted, not yet submitted.
    /// </summary>
    Draft,

    /// <summary>
    ///     Incident has been submitted and is awaiting review.
    /// </summary>
    Submitted,

    /// <summary>
    ///     Incident is currently being reviewed by an administrator.
    /// </summary>
    UnderReview,

    /// <summary>
    ///     Incident review process is complete.
    /// </summary>
    Closed
}