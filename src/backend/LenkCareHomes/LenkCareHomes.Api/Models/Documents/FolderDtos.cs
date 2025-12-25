using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Models.Documents;

// ========== Folder DTOs ==========

/// <summary>
///     DTO for folder summary in list views and tree navigation.
/// </summary>
public sealed record FolderSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DocumentScope Scope { get; init; }
    public Guid? ParentFolderId { get; init; }
    public Guid? ClientId { get; init; }
    public string? ClientName { get; init; }
    public Guid? HomeId { get; init; }
    public string? HomeName { get; init; }
    public bool IsSystemFolder { get; init; }
    public int DocumentCount { get; init; }
    public int SubfolderCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
///     DTO for full folder details including children.
/// </summary>
public sealed record FolderDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DocumentScope Scope { get; init; }
    public Guid? ParentFolderId { get; init; }
    public string? ParentFolderName { get; init; }
    public Guid? ClientId { get; init; }
    public string? ClientName { get; init; }
    public Guid? HomeId { get; init; }
    public string? HomeName { get; init; }
    public bool IsSystemFolder { get; init; }
    public bool IsActive { get; init; }
    public Guid CreatedById { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<FolderSummaryDto> SubFolders { get; init; } = [];
    public IReadOnlyList<DocumentSummaryDto> Documents { get; init; } = [];
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; init; } = [];
}

/// <summary>
///     Breadcrumb item for folder navigation.
/// </summary>
public sealed record BreadcrumbItem
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>
///     Folder tree node for hierarchical display.
/// </summary>
public sealed record FolderTreeNodeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DocumentScope Scope { get; init; }
    public Guid? ParentFolderId { get; init; }
    public Guid? ClientId { get; init; }
    public string? ClientName { get; init; }
    public Guid? HomeId { get; init; }
    public string? HomeName { get; init; }
    public bool IsSystemFolder { get; init; }
    public int DocumentCount { get; init; }
    public IReadOnlyList<FolderTreeNodeDto> Children { get; init; } = [];
}

// ========== Request DTOs ==========

/// <summary>
///     Request to create a new folder.
/// </summary>
public sealed record CreateFolderRequest
{
    public string Name { get; init; } = string.Empty;
    public DocumentScope Scope { get; init; }
    public Guid? ParentFolderId { get; init; }
    public Guid? ClientId { get; init; }
    public Guid? HomeId { get; init; }
}

/// <summary>
///     Request to update a folder.
/// </summary>
public sealed record UpdateFolderRequest
{
    public string? Name { get; init; }
}

/// <summary>
///     Request to move a folder to a new parent.
/// </summary>
public sealed record MoveFolderRequest
{
    public Guid? NewParentFolderId { get; init; }
}

/// <summary>
///     Response from folder operations.
/// </summary>
public sealed record FolderOperationResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public FolderDto? Folder { get; init; }

    public static FolderOperationResponse Ok(FolderDto? folder = null)
    {
        return new FolderOperationResponse { Success = true, Folder = folder };
    }

    public static FolderOperationResponse Fail(string error)
    {
        return new FolderOperationResponse { Success = false, Error = error };
    }
}

// ========== Query Parameters ==========

/// <summary>
///     Query parameters for browsing documents.
/// </summary>
public sealed record BrowseDocumentsQuery
{
    public DocumentScope? Scope { get; init; }
    public Guid? FolderId { get; init; }
    public Guid? ClientId { get; init; }
    public Guid? HomeId { get; init; }
    public DocumentType? DocumentType { get; init; }
    public string? SearchText { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
///     Response for browsing documents with folder context.
/// </summary>
public sealed record BrowseDocumentsResponse
{
    public FolderDto? CurrentFolder { get; init; }
    public IReadOnlyList<FolderSummaryDto> Folders { get; init; } = [];
    public IReadOnlyList<DocumentSummaryDto> Documents { get; init; } = [];
    public int TotalDocuments { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; init; } = [];
}