using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Documents;

namespace LenkCareHomes.Api.Services.Documents;

/// <summary>
///     Service interface for folder management operations.
/// </summary>
public interface IFolderService
{
    /// <summary>
    ///     Creates a new folder.
    /// </summary>
    Task<FolderOperationResponse> CreateFolderAsync(
        CreateFolderRequest request,
        Guid createdById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a folder by ID with its contents.
    /// </summary>
    Task<FolderDto?> GetFolderByIdAsync(
        Guid folderId,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the folder tree for navigation.
    /// </summary>
    Task<IReadOnlyList<FolderTreeNodeDto>> GetFolderTreeAsync(
        DocumentScope? scope,
        Guid? clientId,
        Guid? homeId,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets root-level folders filtered by scope.
    /// </summary>
    Task<IReadOnlyList<FolderSummaryDto>> GetRootFoldersAsync(
        DocumentScope? scope,
        Guid? clientId,
        Guid? homeId,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates a folder.
    /// </summary>
    Task<FolderOperationResponse> UpdateFolderAsync(
        Guid folderId,
        UpdateFolderRequest request,
        Guid updatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Moves a folder to a new parent.
    /// </summary>
    Task<FolderOperationResponse> MoveFolderAsync(
        Guid folderId,
        MoveFolderRequest request,
        Guid movedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a folder (soft delete).
    /// </summary>
    Task<FolderOperationResponse> DeleteFolderAsync(
        Guid folderId,
        Guid deletedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Browses documents with folder/scope filtering.
    /// </summary>
    Task<BrowseDocumentsResponse> BrowseDocumentsAsync(
        BrowseDocumentsQuery query,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets breadcrumb navigation for a folder.
    /// </summary>
    Task<IReadOnlyList<BreadcrumbItem>> GetBreadcrumbsAsync(
        Guid folderId,
        CancellationToken cancellationToken = default);
}