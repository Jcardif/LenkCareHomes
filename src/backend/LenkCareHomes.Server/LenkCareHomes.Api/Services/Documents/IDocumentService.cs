using LenkCareHomes.Api.Models.Documents;

namespace LenkCareHomes.Api.Services.Documents;

/// <summary>
///     Service interface for document management operations.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    ///     Initiates a document upload for a client and returns a SAS URL for uploading.
    /// </summary>
    Task<DocumentUploadResponse> InitiateUploadAsync(
        Guid clientId,
        UploadDocumentRequest request,
        Guid uploadedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Initiates a general document upload (any scope) and returns a SAS URL for uploading.
    /// </summary>
    Task<DocumentUploadResponse> InitiateGeneralUploadAsync(
        UploadDocumentRequest request,
        Guid uploadedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Confirms that an upload was completed.
    /// </summary>
    Task<DocumentOperationResponse> ConfirmUploadAsync(
        Guid documentId,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets documents for a client.
    /// </summary>
    Task<IReadOnlyList<DocumentSummaryDto>> GetDocumentsByClientAsync(
        Guid clientId,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a document by ID.
    /// </summary>
    Task<DocumentDto?> GetDocumentByIdAsync(
        Guid documentId,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a SAS URL for viewing a document.
    /// </summary>
    Task<DocumentViewResponse> GetViewSasUrlAsync(
        Guid documentId,
        Guid userId,
        bool isAdmin,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a document (Admin only).
    /// </summary>
    Task<DocumentOperationResponse> DeleteDocumentAsync(
        Guid documentId,
        Guid deletedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Grants document access to caregivers.
    /// </summary>
    Task<DocumentOperationResponse> GrantAccessAsync(
        Guid documentId,
        GrantDocumentAccessRequest request,
        Guid grantedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Revokes document access from a caregiver.
    /// </summary>
    Task<DocumentOperationResponse> RevokeAccessAsync(
        Guid documentId,
        Guid caregiverId,
        Guid revokedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the access history for a document (grants and revocations).
    /// </summary>
    Task<IReadOnlyList<DocumentAccessHistoryDto>> GetAccessHistoryAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}