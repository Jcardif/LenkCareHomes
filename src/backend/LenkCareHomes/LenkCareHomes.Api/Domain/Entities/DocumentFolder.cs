using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
/// Represents a folder for organizing documents.
/// Supports hierarchical structure with parent-child relationships.
/// </summary>
public sealed class DocumentFolder
{
    /// <summary>
    /// Gets or sets the unique identifier for the folder.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the folder name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the folder.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the scope of this folder (Client, Home, Business, General).
    /// </summary>
    public DocumentScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the parent folder ID for nested folders.
    /// Null for root-level folders.
    /// </summary>
    public Guid? ParentFolderId { get; set; }

    /// <summary>
    /// Gets or sets the client ID if this folder is client-scoped.
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the home ID if this folder is home-scoped.
    /// </summary>
    public Guid? HomeId { get; set; }

    /// <summary>
    /// Gets or sets whether this is a system folder that cannot be deleted.
    /// </summary>
    public bool IsSystemFolder { get; set; }

    /// <summary>
    /// Gets or sets whether the folder is active (not soft-deleted).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the user who created this folder.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Gets or sets when the folder was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the folder was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property for the parent folder.
    /// </summary>
    public DocumentFolder? ParentFolder { get; set; }

    /// <summary>
    /// Navigation property for child folders.
    /// </summary>
    public ICollection<DocumentFolder> ChildFolders { get; set; } = [];

    /// <summary>
    /// Navigation property for documents in this folder.
    /// </summary>
    public ICollection<Document> Documents { get; set; } = [];

    /// <summary>
    /// Navigation property for the client (if client-scoped).
    /// </summary>
    public Client? Client { get; set; }

    /// <summary>
    /// Navigation property for the home (if home-scoped).
    /// </summary>
    public Home? Home { get; set; }

    /// <summary>
    /// Navigation property for the user who created the folder.
    /// </summary>
    public ApplicationUser? CreatedBy { get; set; }
}
