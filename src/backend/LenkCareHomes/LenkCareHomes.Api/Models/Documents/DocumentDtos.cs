using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Models.Documents;

/// <summary>
/// DTO for document summary in list views.
/// </summary>
public sealed record DocumentSummaryDto
{
    public Guid Id { get; init; }
    public DocumentScope Scope { get; init; }
    public Guid? ClientId { get; init; }
    public string? ClientName { get; init; }
    public Guid? HomeId { get; init; }
    public string? HomeName { get; init; }
    public Guid? FolderId { get; init; }
    public string? FolderName { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public DocumentType DocumentType { get; init; }
    public string? Description { get; init; }
    public long FileSizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }
    public string UploadedByName { get; init; } = string.Empty;
    public bool HasAccess { get; init; }
}

/// <summary>
/// DTO for full document details.
/// </summary>
public sealed record DocumentDto
{
    public Guid Id { get; init; }
    public DocumentScope Scope { get; init; }
    public Guid? ClientId { get; init; }
    public string? ClientName { get; init; }
    public Guid? HomeId { get; init; }
    public string? HomeName { get; init; }
    public Guid? FolderId { get; init; }
    public string? FolderName { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public DocumentType DocumentType { get; init; }
    public string? Description { get; init; }
    public long FileSizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }
    public Guid UploadedById { get; init; }
    public string UploadedByName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public IReadOnlyList<DocumentPermissionDto> AccessPermissions { get; init; } = [];
}

/// <summary>
/// DTO for document access permission.
/// </summary>
public sealed record DocumentPermissionDto
{
    public Guid Id { get; init; }
    public Guid CaregiverId { get; init; }
    public string CaregiverName { get; init; } = string.Empty;
    public string CaregiverEmail { get; init; } = string.Empty;
    public Guid GrantedById { get; init; }
    public string GrantedByName { get; init; } = string.Empty;
    public DateTime GrantedDate { get; init; }
}

/// <summary>
/// Request to upload a document.
/// </summary>
public sealed record UploadDocumentRequest
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public DocumentType DocumentType { get; init; }
    public string? Description { get; init; }
    public long FileSizeBytes { get; init; }
    public DocumentScope Scope { get; init; } = DocumentScope.Client;
    public Guid? FolderId { get; init; }
    public Guid? ClientId { get; init; }
    public Guid? HomeId { get; init; }
}

/// <summary>
/// Response containing upload SAS URL.
/// </summary>
public sealed record DocumentUploadResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Guid? DocumentId { get; init; }
    public string? UploadUrl { get; init; }
    public DateTime? ExpiresAt { get; init; }

    public static DocumentUploadResponse Ok(Guid documentId, string uploadUrl, DateTime expiresAt) =>
        new() { Success = true, DocumentId = documentId, UploadUrl = uploadUrl, ExpiresAt = expiresAt };

    public static DocumentUploadResponse Fail(string error) =>
        new() { Success = false, Error = error };
}

/// <summary>
/// Response containing view SAS URL.
/// </summary>
public sealed record DocumentViewResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? ViewUrl { get; init; }
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public DateTime? ExpiresAt { get; init; }

    public static DocumentViewResponse Ok(string viewUrl, string fileName, string contentType, DateTime expiresAt) =>
        new() { Success = true, ViewUrl = viewUrl, FileName = fileName, ContentType = contentType, ExpiresAt = expiresAt };

    public static DocumentViewResponse Fail(string error) =>
        new() { Success = false, Error = error };
}

/// <summary>
/// Request to grant document access to caregivers.
/// </summary>
public sealed record GrantDocumentAccessRequest
{
    public IReadOnlyList<Guid> CaregiverIds { get; init; } = [];
}

/// <summary>
/// DTO for document access history entry.
/// </summary>
public sealed record DocumentAccessHistoryDto
{
    public Guid Id { get; init; }
    public Guid DocumentId { get; init; }
    public Guid CaregiverId { get; init; }
    public string CaregiverName { get; init; } = string.Empty;
    public string CaregiverEmail { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public Guid PerformedById { get; init; }
    public string PerformedByName { get; init; } = string.Empty;
    public DateTime PerformedAt { get; init; }
}

/// <summary>
/// Response from document operations.
/// </summary>
public sealed record DocumentOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public DocumentDto? Document { get; init; }

    public static DocumentOperationResponse Ok(DocumentDto? document = null) =>
        new() { Success = true, Document = document };

    public static DocumentOperationResponse Fail(string error) =>
        new() { Success = false, Error = error };
}
