namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
/// Represents a permission grant for a caregiver to view a specific document.
/// </summary>
public sealed class DocumentAccessPermission
{
    /// <summary>
    /// Gets or sets the unique identifier for the permission.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the document this permission applies to.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the caregiver who has been granted access.
    /// </summary>
    public Guid CaregiverId { get; set; }

    /// <summary>
    /// Gets or sets the admin who granted the permission.
    /// </summary>
    public Guid GrantedById { get; set; }

    /// <summary>
    /// Gets or sets when the permission was granted.
    /// </summary>
    public DateTime GrantedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the document.
    /// </summary>
    public Document? Document { get; set; }

    /// <summary>
    /// Navigation property for the caregiver.
    /// </summary>
    public ApplicationUser? Caregiver { get; set; }

    /// <summary>
    /// Navigation property for the admin who granted access.
    /// </summary>
    public ApplicationUser? GrantedBy { get; set; }
}
