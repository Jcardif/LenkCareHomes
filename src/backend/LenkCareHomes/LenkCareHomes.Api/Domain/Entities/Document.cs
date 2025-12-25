using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents a document stored in the system.
///     The actual file is stored in Azure Blob Storage; this entity contains metadata.
///     Documents can be associated with clients, homes, or be general business documents.
/// </summary>
public sealed class Document
{
    /// <summary>
    ///     Gets or sets the unique identifier for the document.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the scope of this document (Client, Home, Business, General).
    /// </summary>
    public DocumentScope Scope { get; set; } = DocumentScope.Client;

    /// <summary>
    ///     Gets or sets the client this document belongs to (for client-scoped documents).
    /// </summary>
    public Guid? ClientId { get; set; }

    /// <summary>
    ///     Gets or sets the home this document belongs to (for home-scoped documents).
    /// </summary>
    public Guid? HomeId { get; set; }

    /// <summary>
    ///     Gets or sets the folder this document belongs to.
    ///     Null means document is at the root level of its scope.
    /// </summary>
    public Guid? FolderId { get; set; }

    /// <summary>
    ///     Gets or sets the display file name (may be sanitized).
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    ///     Gets or sets the original file name as uploaded.
    /// </summary>
    public required string OriginalFileName { get; set; }

    /// <summary>
    ///     Gets or sets the MIME type of the file.
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    ///     Gets or sets the file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    ///     Gets or sets the type/category of document.
    /// </summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>
    ///     Gets or sets an optional description of the document.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the path to the blob in Azure Blob Storage.
    /// </summary>
    public required string BlobPath { get; set; }

    /// <summary>
    ///     Gets or sets the user who uploaded the document.
    /// </summary>
    public Guid UploadedById { get; set; }

    /// <summary>
    ///     Gets or sets when the document was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets whether the document is active (not soft-deleted).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Gets or sets when the document was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets when the document was last updated.
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
    ///     Navigation property for the folder this document belongs to.
    /// </summary>
    public DocumentFolder? Folder { get; set; }

    /// <summary>
    ///     Navigation property for the user who uploaded the document.
    /// </summary>
    public ApplicationUser? UploadedBy { get; set; }

    /// <summary>
    ///     Navigation property for access permissions.
    /// </summary>
    public ICollection<DocumentAccessPermission> AccessPermissions { get; set; } = [];

    /// <summary>
    ///     Navigation property for access history (grants and revocations).
    /// </summary>
    public ICollection<DocumentAccessHistory> AccessHistory { get; set; } = [];
}