namespace LenkCareHomes.Api.Services.Documents;

/// <summary>
///     Well-known container names for blob storage.
/// </summary>
public static class BlobContainers
{
    /// <summary>
    ///     Container for client documents (PDFs, etc.).
    /// </summary>
    public const string Documents = "documents";

    /// <summary>
    ///     Container for incident photos.
    /// </summary>
    public const string IncidentPhotos = "incident-photos";
}

/// <summary>
///     Service interface for Azure Blob Storage operations.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    ///     Generates a SAS URL for uploading a blob.
    /// </summary>
    /// <param name="blobPath">The path for the blob in storage.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="expirationMinutes">Minutes until the SAS expires (default 5).</param>
    /// <param name="containerName">The container name (default: documents).</param>
    /// <returns>The upload SAS URL and expiration time.</returns>
    Task<(string SasUrl, DateTime ExpiresAt)> GetUploadSasUrlAsync(
        string blobPath,
        string contentType,
        int expirationMinutes = 5,
        string containerName = BlobContainers.Documents);

    /// <summary>
    ///     Generates a SAS URL for reading a blob.
    /// </summary>
    /// <param name="blobPath">The path to the blob in storage.</param>
    /// <param name="expirationMinutes">Minutes until the SAS expires (default 5).</param>
    /// <param name="containerName">The container name (default: documents).</param>
    /// <returns>The read SAS URL and expiration time.</returns>
    Task<(string SasUrl, DateTime ExpiresAt)> GetReadSasUrlAsync(
        string blobPath,
        int expirationMinutes = 5,
        string containerName = BlobContainers.Documents);

    /// <summary>
    ///     Uploads a blob directly from a byte array.
    /// </summary>
    /// <param name="blobPath">The path for the blob in storage.</param>
    /// <param name="content">The file content as bytes.</param>
    /// <param name="contentType">The MIME type of the content.</param>
    /// <param name="containerName">The container name (default: documents).</param>
    /// <returns>Task completing when upload is finished.</returns>
    Task UploadBlobAsync(string blobPath, byte[] content, string contentType,
        string containerName = BlobContainers.Documents);

    /// <summary>
    ///     Uploads a blob directly from a stream.
    /// </summary>
    /// <param name="blobPath">The path for the blob in storage.</param>
    /// <param name="content">The file content as a stream.</param>
    /// <param name="contentType">The MIME type of the content.</param>
    /// <param name="containerName">The container name (default: documents).</param>
    /// <returns>Task completing when upload is finished.</returns>
    Task UploadBlobAsync(string blobPath, Stream content, string contentType,
        string containerName = BlobContainers.Documents);

    /// <summary>
    ///     Deletes a blob from storage.
    /// </summary>
    /// <param name="blobPath">The path to the blob in storage.</param>
    /// <param name="containerName">The container name (default: documents).</param>
    Task DeleteBlobAsync(string blobPath, string containerName = BlobContainers.Documents);

    /// <summary>
    ///     Checks if a blob exists.
    /// </summary>
    /// <param name="blobPath">The path to the blob in storage.</param>
    /// <param name="containerName">The container name (default: documents).</param>
    Task<bool> BlobExistsAsync(string blobPath, string containerName = BlobContainers.Documents);

    /// <summary>
    ///     Downloads a blob's content as a byte array.
    /// </summary>
    /// <param name="blobPath">The path to the blob in storage.</param>
    /// <param name="containerName">The container name (default: documents).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The blob content as bytes.</returns>
    Task<byte[]> DownloadBlobAsync(string blobPath, string containerName = BlobContainers.Documents,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes all blobs in the specified container.
    ///     WARNING: This is destructive and only for development use.
    /// </summary>
    /// <param name="containerName">The container to clear, or null for all known containers.</param>
    /// <returns>The number of blobs deleted.</returns>
    Task<int> DeleteAllBlobsAsync(string? containerName = null);
}