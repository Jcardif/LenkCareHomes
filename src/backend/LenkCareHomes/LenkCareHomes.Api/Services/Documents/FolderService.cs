using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Documents;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.Documents;

/// <summary>
/// Service for folder management operations.
/// </summary>
public sealed class FolderService : IFolderService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<FolderService> _logger;

    public FolderService(
        ApplicationDbContext dbContext,
        IAuditLogService auditLogService,
        ILogger<FolderService> logger)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FolderOperationResponse> CreateFolderAsync(
        CreateFolderRequest request,
        Guid createdById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return FolderOperationResponse.Fail("Folder name is required.");
        }

        // Validate scope-specific requirements
        if (request.Scope == DocumentScope.Client && request.ClientId is null)
        {
            return FolderOperationResponse.Fail("Client ID is required for client-scoped folders.");
        }

        if (request.Scope == DocumentScope.Home && request.HomeId is null)
        {
            return FolderOperationResponse.Fail("Home ID is required for home-scoped folders.");
        }

        // Validate parent folder if specified
        if (request.ParentFolderId is not null)
        {
            var parentFolder = await _dbContext.DocumentFolders
                .FirstOrDefaultAsync(f => f.Id == request.ParentFolderId && f.IsActive, cancellationToken);

            if (parentFolder is null)
            {
                return FolderOperationResponse.Fail("Parent folder not found.");
            }

            // Ensure scope matches parent
            if (parentFolder.Scope != request.Scope)
            {
                return FolderOperationResponse.Fail("Folder scope must match parent folder scope.");
            }
        }

        // Check for duplicate name in same location
        var exists = await _dbContext.DocumentFolders
            .AnyAsync(f =>
                f.Name == request.Name &&
                f.ParentFolderId == request.ParentFolderId &&
                f.Scope == request.Scope &&
                f.ClientId == request.ClientId &&
                f.HomeId == request.HomeId &&
                f.IsActive,
                cancellationToken);

        if (exists)
        {
            return FolderOperationResponse.Fail("A folder with this name already exists in this location.");
        }

        var folder = new DocumentFolder
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Scope = request.Scope,
            ParentFolderId = request.ParentFolderId,
            ClientId = request.ClientId,
            HomeId = request.HomeId,
            CreatedById = createdById,
            IsSystemFolder = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.DocumentFolders.Add(folder);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Log audit event
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == createdById, cancellationToken);
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.FolderCreated,
            createdById,
            user?.Email ?? "Unknown",
            "DocumentFolder",
            folder.Id.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Folder '{folder.Name}' created with scope {folder.Scope}",
            cancellationToken);

        var dto = await GetFolderByIdAsync(folder.Id, createdById, true, null, cancellationToken);
        return FolderOperationResponse.Ok(dto);
    }

    /// <inheritdoc />
    public async Task<FolderDto?> GetFolderByIdAsync(
        Guid folderId,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var folder = await _dbContext.DocumentFolders
            .Include(f => f.ParentFolder)
            .Include(f => f.Client)
            .Include(f => f.Home)
            .Include(f => f.CreatedBy)
            .Include(f => f.ChildFolders.Where(c => c.IsActive))
            .Include(f => f.Documents.Where(d => d.IsActive))
                .ThenInclude(d => d.UploadedBy)
            .Include(f => f.Documents.Where(d => d.IsActive))
                .ThenInclude(d => d.AccessPermissions)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == folderId && f.IsActive, cancellationToken);

        if (folder is null)
        {
            return null;
        }

        // Check access for caregivers
        if (!isAdmin && folder.HomeId is not null && allowedHomeIds is not null)
        {
            if (!allowedHomeIds.Contains(folder.HomeId.Value))
            {
                return null;
            }
        }

        var breadcrumbs = await GetBreadcrumbsAsync(folderId, cancellationToken);

        return MapToFolderDto(folder, userId, isAdmin, breadcrumbs);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FolderTreeNodeDto>> GetFolderTreeAsync(
        DocumentScope? scope,
        Guid? clientId,
        Guid? homeId,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DocumentFolders
            .Include(f => f.Client)
            .Include(f => f.Home)
            .Include(f => f.Documents.Where(d => d.IsActive))
            .Where(f => f.IsActive)
            .AsNoTracking();

        // Apply scope filter
        if (scope is not null)
        {
            query = query.Where(f => f.Scope == scope);
        }

        if (clientId is not null)
        {
            query = query.Where(f => f.ClientId == clientId);
        }

        if (homeId is not null)
        {
            query = query.Where(f => f.HomeId == homeId);
        }

        // Apply home scope for caregivers
        if (!isAdmin && allowedHomeIds is not null)
        {
            query = query.Where(f =>
                f.Scope == DocumentScope.General ||
                f.Scope == DocumentScope.Business ||
                (f.HomeId != null && allowedHomeIds.Contains(f.HomeId.Value)) ||
                (f.ClientId != null && _dbContext.Clients.Any(c => c.Id == f.ClientId && allowedHomeIds.Contains(c.HomeId))));
        }

        var folders = await query.ToListAsync(cancellationToken);

        // Build tree structure
        return BuildFolderTree(folders, null);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FolderSummaryDto>> GetRootFoldersAsync(
        DocumentScope? scope,
        Guid? clientId,
        Guid? homeId,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DocumentFolders
            .Include(f => f.Client)
            .Include(f => f.Home)
            .Include(f => f.ChildFolders.Where(c => c.IsActive))
            .Include(f => f.Documents.Where(d => d.IsActive))
            .Where(f => f.IsActive && f.ParentFolderId == null)
            .AsNoTracking();

        if (scope is not null)
        {
            query = query.Where(f => f.Scope == scope);
        }

        if (clientId is not null)
        {
            query = query.Where(f => f.ClientId == clientId);
        }

        if (homeId is not null)
        {
            query = query.Where(f => f.HomeId == homeId);
        }

        // Apply home scope for caregivers
        if (!isAdmin && allowedHomeIds is not null)
        {
            query = query.Where(f =>
                f.Scope == DocumentScope.General ||
                f.Scope == DocumentScope.Business ||
                (f.HomeId != null && allowedHomeIds.Contains(f.HomeId.Value)) ||
                (f.ClientId != null && _dbContext.Clients.Any(c => c.Id == f.ClientId && allowedHomeIds.Contains(c.HomeId))));
        }

        var folders = await query.OrderBy(f => f.Name).ToListAsync(cancellationToken);

        return folders.Select(MapToFolderSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<FolderOperationResponse> UpdateFolderAsync(
        Guid folderId,
        UpdateFolderRequest request,
        Guid updatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var folder = await _dbContext.DocumentFolders
            .FirstOrDefaultAsync(f => f.Id == folderId && f.IsActive, cancellationToken);

        if (folder is null)
        {
            return FolderOperationResponse.Fail("Folder not found.");
        }

        if (folder.IsSystemFolder)
        {
            return FolderOperationResponse.Fail("System folders cannot be modified.");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            // Check for duplicate name
            var exists = await _dbContext.DocumentFolders
                .AnyAsync(f =>
                    f.Id != folderId &&
                    f.Name == request.Name &&
                    f.ParentFolderId == folder.ParentFolderId &&
                    f.Scope == folder.Scope &&
                    f.ClientId == folder.ClientId &&
                    f.HomeId == folder.HomeId &&
                    f.IsActive,
                    cancellationToken);

            if (exists)
            {
                return FolderOperationResponse.Fail("A folder with this name already exists in this location.");
            }

            folder.Name = request.Name.Trim();
        }

        folder.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Log audit event
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == updatedById, cancellationToken);
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.FolderUpdated,
            updatedById,
            user?.Email ?? "Unknown",
            "DocumentFolder",
            folder.Id.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Folder '{folder.Name}' updated",
            cancellationToken);

        var dto = await GetFolderByIdAsync(folder.Id, updatedById, true, null, cancellationToken);
        return FolderOperationResponse.Ok(dto);
    }

    /// <inheritdoc />
    public async Task<FolderOperationResponse> MoveFolderAsync(
        Guid folderId,
        MoveFolderRequest request,
        Guid movedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var folder = await _dbContext.DocumentFolders
            .FirstOrDefaultAsync(f => f.Id == folderId && f.IsActive, cancellationToken);

        if (folder is null)
        {
            return FolderOperationResponse.Fail("Folder not found.");
        }

        if (folder.IsSystemFolder)
        {
            return FolderOperationResponse.Fail("System folders cannot be moved.");
        }

        // Validate new parent
        if (request.NewParentFolderId is not null)
        {
            if (request.NewParentFolderId == folderId)
            {
                return FolderOperationResponse.Fail("Cannot move folder into itself.");
            }

            var newParent = await _dbContext.DocumentFolders
                .FirstOrDefaultAsync(f => f.Id == request.NewParentFolderId && f.IsActive, cancellationToken);

            if (newParent is null)
            {
                return FolderOperationResponse.Fail("Target folder not found.");
            }

            if (newParent.Scope != folder.Scope)
            {
                return FolderOperationResponse.Fail("Cannot move folder to a different scope.");
            }

            // Check for circular reference
            if (await IsDescendantOfAsync(request.NewParentFolderId.Value, folderId, cancellationToken))
            {
                return FolderOperationResponse.Fail("Cannot move folder into its own subfolder.");
            }
        }

        folder.ParentFolderId = request.NewParentFolderId;
        folder.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Log audit event
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == movedById, cancellationToken);
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.FolderMoved,
            movedById,
            user?.Email ?? "Unknown",
            "DocumentFolder",
            folder.Id.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Folder '{folder.Name}' moved to new location",
            cancellationToken);

        var dto = await GetFolderByIdAsync(folder.Id, movedById, true, null, cancellationToken);
        return FolderOperationResponse.Ok(dto);
    }

    /// <inheritdoc />
    public async Task<FolderOperationResponse> DeleteFolderAsync(
        Guid folderId,
        Guid deletedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var folder = await _dbContext.DocumentFolders
            .Include(f => f.ChildFolders.Where(c => c.IsActive))
            .Include(f => f.Documents.Where(d => d.IsActive))
            .FirstOrDefaultAsync(f => f.Id == folderId && f.IsActive, cancellationToken);

        if (folder is null)
        {
            return FolderOperationResponse.Fail("Folder not found.");
        }

        if (folder.IsSystemFolder)
        {
            return FolderOperationResponse.Fail("System folders cannot be deleted.");
        }

        if (folder.ChildFolders.Count > 0)
        {
            return FolderOperationResponse.Fail("Cannot delete folder with subfolders. Please delete or move subfolders first.");
        }

        if (folder.Documents.Count > 0)
        {
            return FolderOperationResponse.Fail("Cannot delete folder with documents. Please delete or move documents first.");
        }

        folder.IsActive = false;
        folder.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Log audit event
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == deletedById, cancellationToken);
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.FolderDeleted,
            deletedById,
            user?.Email ?? "Unknown",
            "DocumentFolder",
            folder.Id.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Folder '{folder.Name}' deleted",
            cancellationToken);

        return FolderOperationResponse.Ok();
    }

    /// <inheritdoc />
    public async Task<BrowseDocumentsResponse> BrowseDocumentsAsync(
        BrowseDocumentsQuery query,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // For Client and Home scopes at root level (no folder, no clientId/homeId selected),
        // show clients/homes as virtual folders
        if (query.FolderId is null && query.Scope == DocumentScope.Client && query.ClientId is null)
        {
            return await BrowseClientsAsVirtualFoldersAsync(query, userId, isAdmin, allowedHomeIds, cancellationToken);
        }

        if (query.FolderId is null && query.Scope == DocumentScope.Home && query.HomeId is null)
        {
            return await BrowseHomesAsVirtualFoldersAsync(query, userId, isAdmin, allowedHomeIds, cancellationToken);
        }

        FolderDto? currentFolder = null;
        IReadOnlyList<BreadcrumbItem> breadcrumbs = [];

        // If folder is specified, get folder details
        if (query.FolderId is not null)
        {
            currentFolder = await GetFolderByIdAsync(query.FolderId.Value, userId, isAdmin, allowedHomeIds, cancellationToken);
            if (currentFolder is not null)
            {
                breadcrumbs = currentFolder.Breadcrumbs;
            }
        }
        else
        {
            // Build breadcrumbs based on scope and client/home selection
            breadcrumbs = await BuildBreadcrumbsForContextAsync(query.Scope, query.ClientId, query.HomeId, cancellationToken);
        }

        // Get folders at current level
        var foldersQuery = _dbContext.DocumentFolders
            .Include(f => f.Client)
            .Include(f => f.Home)
            .Include(f => f.ChildFolders.Where(c => c.IsActive))
            .Include(f => f.Documents.Where(d => d.IsActive))
            .Where(f => f.IsActive && f.ParentFolderId == query.FolderId)
            .AsNoTracking();

        // Apply filters
        if (query.Scope is not null)
        {
            foldersQuery = foldersQuery.Where(f => f.Scope == query.Scope);
        }

        if (query.ClientId is not null)
        {
            foldersQuery = foldersQuery.Where(f => f.ClientId == query.ClientId);
        }

        if (query.HomeId is not null)
        {
            foldersQuery = foldersQuery.Where(f => f.HomeId == query.HomeId);
        }

        // Apply home scope for caregivers
        if (!isAdmin && allowedHomeIds is not null)
        {
            foldersQuery = foldersQuery.Where(f =>
                f.Scope == DocumentScope.General ||
                f.Scope == DocumentScope.Business ||
                (f.HomeId != null && allowedHomeIds.Contains(f.HomeId.Value)) ||
                (f.ClientId != null && _dbContext.Clients.Any(c => c.Id == f.ClientId && allowedHomeIds.Contains(c.HomeId))));
        }

        var folders = await foldersQuery.OrderBy(f => f.Name).ToListAsync(cancellationToken);

        // Get documents at current level
        var documentsQuery = _dbContext.Documents
            .Include(d => d.Client)
            .Include(d => d.Home)
            .Include(d => d.Folder)
            .Include(d => d.UploadedBy)
            .Include(d => d.AccessPermissions)
            .Where(d => d.IsActive && d.FolderId == query.FolderId)
            .AsNoTracking();

        // Apply filters
        if (query.Scope is not null)
        {
            documentsQuery = documentsQuery.Where(d => d.Scope == query.Scope);
        }

        if (query.ClientId is not null)
        {
            documentsQuery = documentsQuery.Where(d => d.ClientId == query.ClientId);
        }

        if (query.HomeId is not null)
        {
            documentsQuery = documentsQuery.Where(d => d.HomeId == query.HomeId);
        }

        if (query.DocumentType is not null)
        {
            documentsQuery = documentsQuery.Where(d => d.DocumentType == query.DocumentType);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var searchLower = query.SearchText.ToLower();
            documentsQuery = documentsQuery.Where(d =>
                d.FileName.ToLower().Contains(searchLower) ||
                d.OriginalFileName.ToLower().Contains(searchLower) ||
                (d.Description != null && d.Description.ToLower().Contains(searchLower)));
        }

        // Apply home scope for caregivers
        if (!isAdmin && allowedHomeIds is not null)
        {
            documentsQuery = documentsQuery.Where(d =>
                d.Scope == DocumentScope.General ||
                d.Scope == DocumentScope.Business ||
                (d.HomeId != null && allowedHomeIds.Contains(d.HomeId.Value)) ||
                (d.ClientId != null && _dbContext.Clients.Any(c => c.Id == d.ClientId && allowedHomeIds.Contains(c.HomeId))));
        }

        var totalDocuments = await documentsQuery.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalDocuments / (double)query.PageSize);

        var documents = await documentsQuery
            .OrderByDescending(d => d.UploadedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new BrowseDocumentsResponse
        {
            CurrentFolder = currentFolder,
            Folders = folders.Select(MapToFolderSummary).ToList(),
            Documents = documents.Select(d => MapToDocumentSummary(d, userId, isAdmin)).ToList(),
            TotalDocuments = totalDocuments,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = totalPages,
            HasNextPage = query.PageNumber < totalPages,
            HasPreviousPage = query.PageNumber > 1,
            Breadcrumbs = breadcrumbs
        };
    }

    /// <summary>
    /// Browse clients as virtual folders for Client scope.
    /// </summary>
    private async Task<BrowseDocumentsResponse> BrowseClientsAsVirtualFoldersAsync(
        BrowseDocumentsQuery query,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds,
        CancellationToken cancellationToken)
    {
        var clientsQuery = _dbContext.Clients
            .Include(c => c.Home)
            .Where(c => c.IsActive)
            .AsNoTracking();

        // Apply home scope for caregivers
        if (!isAdmin && allowedHomeIds is not null)
        {
            clientsQuery = clientsQuery.Where(c => allowedHomeIds.Contains(c.HomeId));
        }

        var clients = await clientsQuery.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync(cancellationToken);

        // Get document counts per client
        var clientIds = clients.Select(c => c.Id).ToList();
        var documentCounts = await _dbContext.Documents
            .Where(d => d.IsActive && d.Scope == DocumentScope.Client && d.ClientId != null && clientIds.Contains(d.ClientId.Value))
            .GroupBy(d => d.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId!.Value, x => x.Count, cancellationToken);

        // Map clients to virtual folder summaries
        var virtualFolders = clients.Select(c => new FolderSummaryDto
        {
            Id = c.Id, // Use client ID as folder ID
            Name = $"{c.FirstName} {c.LastName}",
            Scope = DocumentScope.Client,
            ParentFolderId = null,
            ClientId = c.Id,
            ClientName = $"{c.FirstName} {c.LastName}",
            HomeId = c.HomeId,
            HomeName = c.Home?.Name,
            IsSystemFolder = true, // Mark as system so can't be deleted
            DocumentCount = documentCounts.GetValueOrDefault(c.Id, 0),
            SubfolderCount = 0,
            CreatedAt = c.CreatedAt
        }).ToList();

        return new BrowseDocumentsResponse
        {
            CurrentFolder = null,
            Folders = virtualFolders,
            Documents = [],
            TotalDocuments = 0,
            PageNumber = 1,
            PageSize = query.PageSize,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = false,
            Breadcrumbs = BuildRootBreadcrumb(DocumentScope.Client)
        };
    }

    /// <summary>
    /// Browse homes as virtual folders for Home scope.
    /// </summary>
    private async Task<BrowseDocumentsResponse> BrowseHomesAsVirtualFoldersAsync(
        BrowseDocumentsQuery query,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds,
        CancellationToken cancellationToken)
    {
        var homesQuery = _dbContext.Homes
            .Where(h => h.IsActive)
            .AsNoTracking();

        // Apply home scope for caregivers
        if (!isAdmin && allowedHomeIds is not null)
        {
            homesQuery = homesQuery.Where(h => allowedHomeIds.Contains(h.Id));
        }

        var homes = await homesQuery.OrderBy(h => h.Name).ToListAsync(cancellationToken);

        // Get document counts per home
        var homeIds = homes.Select(h => h.Id).ToList();
        var documentCounts = await _dbContext.Documents
            .Where(d => d.IsActive && d.Scope == DocumentScope.Home && d.HomeId != null && homeIds.Contains(d.HomeId.Value))
            .GroupBy(d => d.HomeId)
            .Select(g => new { HomeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.HomeId!.Value, x => x.Count, cancellationToken);

        // Map homes to virtual folder summaries
        var virtualFolders = homes.Select(h => new FolderSummaryDto
        {
            Id = h.Id, // Use home ID as folder ID
            Name = h.Name,
            Scope = DocumentScope.Home,
            ParentFolderId = null,
            ClientId = null,
            ClientName = null,
            HomeId = h.Id,
            HomeName = h.Name,
            IsSystemFolder = true, // Mark as system so can't be deleted
            DocumentCount = documentCounts.GetValueOrDefault(h.Id, 0),
            SubfolderCount = 0,
            CreatedAt = h.CreatedAt
        }).ToList();

        return new BrowseDocumentsResponse
        {
            CurrentFolder = null,
            Folders = virtualFolders,
            Documents = [],
            TotalDocuments = 0,
            PageNumber = 1,
            PageSize = query.PageSize,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = false,
            Breadcrumbs = BuildRootBreadcrumb(DocumentScope.Home)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BreadcrumbItem>> GetBreadcrumbsAsync(
        Guid folderId,
        CancellationToken cancellationToken = default)
    {
        var breadcrumbs = new List<BreadcrumbItem>();
        var currentId = folderId;

        while (true)
        {
            var folder = await _dbContext.DocumentFolders
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == currentId, cancellationToken);

            if (folder is null)
            {
                break;
            }

            breadcrumbs.Insert(0, new BreadcrumbItem
            {
                Id = folder.Id,
                Name = folder.Name
            });

            if (folder.ParentFolderId is null)
            {
                break;
            }

            currentId = folder.ParentFolderId.Value;
        }

        // Add root
        breadcrumbs.Insert(0, new BreadcrumbItem { Id = null, Name = "Documents" });

        return breadcrumbs;
    }

    private async Task<bool> IsDescendantOfAsync(Guid folderId, Guid potentialParentId, CancellationToken cancellationToken)
    {
        var currentId = folderId;

        while (true)
        {
            var folder = await _dbContext.DocumentFolders
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == currentId, cancellationToken);

            if (folder?.ParentFolderId is null)
            {
                return false;
            }

            if (folder.ParentFolderId == potentialParentId)
            {
                return true;
            }

            currentId = folder.ParentFolderId.Value;
        }
    }

    private static IReadOnlyList<BreadcrumbItem> BuildRootBreadcrumb(DocumentScope? scope)
    {
        var name = scope switch
        {
            DocumentScope.Client => "Client Documents",
            DocumentScope.Home => "Facility Documents",
            DocumentScope.Business => "Business Documents",
            DocumentScope.General => "General Documents",
            _ => "Documents"
        };

        return [new BreadcrumbItem { Id = null, Name = name }];
    }

    /// <summary>
    /// Build breadcrumbs based on scope and client/home context.
    /// </summary>
    private async Task<IReadOnlyList<BreadcrumbItem>> BuildBreadcrumbsForContextAsync(
        DocumentScope? scope,
        Guid? clientId,
        Guid? homeId,
        CancellationToken cancellationToken)
    {
        var breadcrumbs = new List<BreadcrumbItem>();

        // Root breadcrumb for the scope
        var scopeName = scope switch
        {
            DocumentScope.Client => "Client Documents",
            DocumentScope.Home => "Facility (Home)",
            DocumentScope.Business => "Business Documents",
            DocumentScope.General => "General Documents",
            _ => "Documents"
        };
        breadcrumbs.Add(new BreadcrumbItem { Id = null, Name = scopeName });

        // Add client breadcrumb if client is selected
        if (scope == DocumentScope.Client && clientId.HasValue)
        {
            var client = await _dbContext.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == clientId.Value, cancellationToken);
            
            if (client is not null)
            {
                breadcrumbs.Add(new BreadcrumbItem 
                { 
                    Id = client.Id, 
                    Name = $"{client.FirstName} {client.LastName}" 
                });
            }
        }

        // Add home breadcrumb if home is selected
        if (scope == DocumentScope.Home && homeId.HasValue)
        {
            var home = await _dbContext.Homes
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == homeId.Value, cancellationToken);
            
            if (home is not null)
            {
                breadcrumbs.Add(new BreadcrumbItem 
                { 
                    Id = home.Id, 
                    Name = home.Name 
                });
            }
        }

        return breadcrumbs;
    }

    private IReadOnlyList<FolderTreeNodeDto> BuildFolderTree(List<DocumentFolder> folders, Guid? parentId)
    {
        return folders
            .Where(f => f.ParentFolderId == parentId)
            .OrderBy(f => f.Name)
            .Select(f => new FolderTreeNodeDto
            {
                Id = f.Id,
                Name = f.Name,
                Scope = f.Scope,
                ParentFolderId = f.ParentFolderId,
                ClientId = f.ClientId,
                ClientName = f.Client is not null ? $"{f.Client.FirstName} {f.Client.LastName}" : null,
                HomeId = f.HomeId,
                HomeName = f.Home?.Name,
                IsSystemFolder = f.IsSystemFolder,
                DocumentCount = f.Documents.Count,
                Children = BuildFolderTree(folders, f.Id)
            })
            .ToList();
    }

    private static FolderSummaryDto MapToFolderSummary(DocumentFolder folder)
    {
        return new FolderSummaryDto
        {
            Id = folder.Id,
            Name = folder.Name,
            Scope = folder.Scope,
            ParentFolderId = folder.ParentFolderId,
            ClientId = folder.ClientId,
            ClientName = folder.Client is not null ? $"{folder.Client.FirstName} {folder.Client.LastName}" : null,
            HomeId = folder.HomeId,
            HomeName = folder.Home?.Name,
            IsSystemFolder = folder.IsSystemFolder,
            DocumentCount = folder.Documents.Count,
            SubfolderCount = folder.ChildFolders.Count,
            CreatedAt = folder.CreatedAt
        };
    }

    private FolderDto MapToFolderDto(DocumentFolder folder, Guid userId, bool isAdmin, IReadOnlyList<BreadcrumbItem> breadcrumbs)
    {
        return new FolderDto
        {
            Id = folder.Id,
            Name = folder.Name,
            Scope = folder.Scope,
            ParentFolderId = folder.ParentFolderId,
            ParentFolderName = folder.ParentFolder?.Name,
            ClientId = folder.ClientId,
            ClientName = folder.Client is not null ? $"{folder.Client.FirstName} {folder.Client.LastName}" : null,
            HomeId = folder.HomeId,
            HomeName = folder.Home?.Name,
            IsSystemFolder = folder.IsSystemFolder,
            IsActive = folder.IsActive,
            CreatedById = folder.CreatedById,
            CreatedByName = folder.CreatedBy is not null
                ? $"{folder.CreatedBy.FirstName} {folder.CreatedBy.LastName}"
                : "Unknown",
            CreatedAt = folder.CreatedAt,
            UpdatedAt = folder.UpdatedAt,
            SubFolders = folder.ChildFolders
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(MapToFolderSummary)
                .ToList(),
            Documents = folder.Documents
                .Where(d => d.IsActive)
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => MapToDocumentSummary(d, userId, isAdmin))
                .ToList(),
            Breadcrumbs = breadcrumbs
        };
    }

    private static DocumentSummaryDto MapToDocumentSummary(Document document, Guid userId, bool isAdmin)
    {
        return new DocumentSummaryDto
        {
            Id = document.Id,
            Scope = document.Scope,
            ClientId = document.ClientId,
            ClientName = document.Client is not null
                ? $"{document.Client.FirstName} {document.Client.LastName}"
                : null,
            HomeId = document.HomeId,
            HomeName = document.Home?.Name,
            FolderId = document.FolderId,
            FolderName = document.Folder?.Name,
            FileName = document.FileName,
            ContentType = document.ContentType,
            DocumentType = document.DocumentType,
            Description = document.Description,
            FileSizeBytes = document.FileSizeBytes,
            UploadedAt = document.UploadedAt,
            UploadedByName = document.UploadedBy is not null
                ? $"{document.UploadedBy.FirstName} {document.UploadedBy.LastName}"
                : "Unknown",
            HasAccess = isAdmin || document.AccessPermissions.Any(p => p.CaregiverId == userId)
        };
    }
}
