namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Tracks the history of document access permission changes (grants and revocations).
/// </summary>
public sealed class DocumentAccessHistory
{
    /// <summary>
    ///     Gets or sets the unique identifier for the history entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the document this history entry relates to.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    ///     Gets or sets the caregiver whose access was affected.
    /// </summary>
    public Guid CaregiverId { get; set; }

    /// <summary>
    ///     Gets or sets the action performed: "Granted" or "Revoked".
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    ///     Gets or sets the admin who performed the action.
    /// </summary>
    public Guid PerformedById { get; set; }

    /// <summary>
    ///     Gets or sets when the action was performed.
    /// </summary>
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Navigation property for the document.
    /// </summary>
    public Document? Document { get; set; }

    /// <summary>
    ///     Navigation property for the caregiver.
    /// </summary>
    public ApplicationUser? Caregiver { get; set; }

    /// <summary>
    ///     Navigation property for the admin who performed the action.
    /// </summary>
    public ApplicationUser? PerformedBy { get; set; }
}