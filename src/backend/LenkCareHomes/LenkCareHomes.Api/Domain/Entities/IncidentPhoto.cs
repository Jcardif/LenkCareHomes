namespace LenkCareHomes.Api.Domain.Entities;

/// <summary>
///     Represents a photo attached to an incident report.
///     Photos are stored in Azure Blob Storage with references in this table.
/// </summary>
public sealed class IncidentPhoto
{
    /// <summary>
    ///     Gets or sets the unique identifier for the photo.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the incident this photo belongs to.
    /// </summary>
    public Guid IncidentId { get; set; }

    /// <summary>
    ///     Gets or sets the path to the blob in Azure Storage.
    /// </summary>
    public required string BlobPath { get; set; }

    /// <summary>
    ///     Gets or sets the original file name.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    ///     Gets or sets the MIME content type (e.g., image/jpeg).
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    ///     Gets or sets the file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    ///     Gets or sets the display order of the photo (0-based).
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    ///     Gets or sets optional caption or description for the photo.
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    ///     Gets or sets when the photo was uploaded.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the user who uploaded the photo.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    ///     Navigation property for the incident.
    /// </summary>
    public Incident? Incident { get; set; }

    /// <summary>
    ///     Navigation property for the uploader.
    /// </summary>
    public ApplicationUser? CreatedBy { get; set; }
}