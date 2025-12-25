using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Models.Documents;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LenkCareHomes.Api.Services.Documents;

/// <summary>
/// Service for document management operations.
/// </summary>
public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IAuditLogService _auditLogService;
    private readonly BlobStorageSettings _blobSettings;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        IAuditLogService auditLogService,
        IOptions<BlobStorageSettings> blobSettings,
        ILogger<DocumentService> logger)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
        _auditLogService = auditLogService;
        _blobSettings = blobSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DocumentUploadResponse> InitiateUploadAsync(
        Guid clientId,
        UploadDocumentRequest request,
        Guid uploadedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate request
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return DocumentUploadResponse.Fail("File name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            return DocumentUploadResponse.Fail("Content type is required.");
        }

        if (request.FileSizeBytes <= 0)
        {
            return DocumentUploadResponse.Fail("File size must be greater than 0.");
        }

        if (request.FileSizeBytes > _blobSettings.MaxFileSizeBytes)
        {
            return DocumentUploadResponse.Fail($"File size exceeds maximum allowed ({_blobSettings.MaxFileSizeBytes / 1024 / 1024}MB).");
        }

        // Verify client exists
        var client = await _dbContext.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

        if (client is null)
        {
            return DocumentUploadResponse.Fail("Client not found.");
        }

        // Generate unique blob filename: {name}_{type}_{checksum}.{ext}
        // Example: care_plan_CarePlan_A7B3.pdf
        var documentId = Guid.NewGuid();
        var blobFileName = GenerateBlobFileName(request.FileName, request.DocumentType, documentId);
        var blobPath = $"clients/{clientId}/{blobFileName}";

        // Create document record
        var document = new Document
        {
            Id = documentId,
            ClientId = clientId,
            FileName = blobFileName,
            OriginalFileName = request.FileName,
            BlobPath = blobPath,
            ContentType = request.ContentType,
            DocumentType = request.DocumentType,
            Description = request.Description,
            UploadedById = uploadedById,
            FileSizeBytes = request.FileSizeBytes,
            UploadedAt = DateTime.UtcNow,
            IsActive = false // Will be set to true after upload confirmation
        };

        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Generate upload SAS URL
        var (sasUrl, expiresAt) = await _blobStorageService.GetUploadSasUrlAsync(
            blobPath,
            request.ContentType);

        // Get uploader for audit log
        var uploader = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == uploadedById, cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.DocumentUploaded,
            uploadedById,
            uploader?.Email ?? "Unknown",
            "Document",
            documentId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Upload initiated for document '{request.FileName}' for client {client.FullName}",
            cancellationToken);

        return DocumentUploadResponse.Ok(documentId, sasUrl, expiresAt);
    }

    /// <inheritdoc />
    public async Task<DocumentUploadResponse> InitiateGeneralUploadAsync(
        UploadDocumentRequest request,
        Guid uploadedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate common fields
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return DocumentUploadResponse.Fail("File name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            return DocumentUploadResponse.Fail("Content type is required.");
        }

        if (request.FileSizeBytes <= 0)
        {
            return DocumentUploadResponse.Fail("File size must be greater than 0.");
        }

        if (request.FileSizeBytes > _blobSettings.MaxFileSizeBytes)
        {
            return DocumentUploadResponse.Fail($"File size exceeds maximum allowed ({_blobSettings.MaxFileSizeBytes / 1024 / 1024}MB).");
        }

        // Validate scope-specific requirements
        var scope = request.Scope;
        string? contextInfo = null;

        switch (scope)
        {
            case Domain.Enums.DocumentScope.Client:
                if (!request.ClientId.HasValue)
                {
                    return DocumentUploadResponse.Fail("ClientId is required for client-scoped documents.");
                }
                var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.Id == request.ClientId.Value, cancellationToken);
                if (client is null)
                {
                    return DocumentUploadResponse.Fail("Client not found.");
                }
                contextInfo = $"for client {client.FullName}";
                break;

            case Domain.Enums.DocumentScope.Home:
                if (!request.HomeId.HasValue)
                {
                    return DocumentUploadResponse.Fail("HomeId is required for home-scoped documents.");
                }
                var home = await _dbContext.Homes.FirstOrDefaultAsync(h => h.Id == request.HomeId.Value, cancellationToken);
                if (home is null)
                {
                    return DocumentUploadResponse.Fail("Home not found.");
                }
                contextInfo = $"for home {home.Name}";
                break;

            case Domain.Enums.DocumentScope.Business:
                contextInfo = "for business documents";
                break;

            case Domain.Enums.DocumentScope.General:
            default:
                contextInfo = "as general document";
                break;
        }

        // Validate folder if provided
        if (request.FolderId.HasValue)
        {
            var folder = await _dbContext.DocumentFolders
                .FirstOrDefaultAsync(f => f.Id == request.FolderId.Value, cancellationToken);
            if (folder is null)
            {
                return DocumentUploadResponse.Fail("Folder not found.");
            }
            // Folder scope must match document scope
            if (folder.Scope != scope)
            {
                return DocumentUploadResponse.Fail("Document scope must match folder scope.");
            }
        }

        // Generate unique blob filename
        var documentId = Guid.NewGuid();
        var blobFileName = GenerateBlobFileName(request.FileName, request.DocumentType, documentId);
        var blobPath = GenerateBlobPath(scope, request.ClientId, request.HomeId, blobFileName);

        // Create document record
        var document = new Document
        {
            Id = documentId,
            Scope = scope,
            ClientId = request.ClientId,
            HomeId = request.HomeId,
            FolderId = request.FolderId,
            FileName = blobFileName,
            OriginalFileName = request.FileName,
            BlobPath = blobPath,
            ContentType = request.ContentType,
            DocumentType = request.DocumentType,
            Description = request.Description,
            UploadedById = uploadedById,
            FileSizeBytes = request.FileSizeBytes,
            UploadedAt = DateTime.UtcNow,
            IsActive = false // Will be set to true after upload confirmation
        };

        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Generate upload SAS URL
        var (sasUrl, expiresAt) = await _blobStorageService.GetUploadSasUrlAsync(
            blobPath,
            request.ContentType);

        // Get uploader for audit log
        var uploader = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == uploadedById, cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.DocumentUploaded,
            uploadedById,
            uploader?.Email ?? "Unknown",
            "Document",
            documentId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Upload initiated for document '{request.FileName}' ({scope}) {contextInfo}",
            cancellationToken);

        return DocumentUploadResponse.Ok(documentId, sasUrl, expiresAt);
    }

    /// <inheritdoc />
    public async Task<DocumentOperationResponse> ConfirmUploadAsync(
        Guid documentId,
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            return DocumentOperationResponse.Fail("Document not found.");
        }

        if (document.IsActive)
        {
            return DocumentOperationResponse.Fail("Document upload already confirmed.");
        }

        // Verify blob exists
        var blobExists = await _blobStorageService.BlobExistsAsync(document.BlobPath);
        _logger.LogInformation(
            "Confirm upload for document {DocumentId}: BlobPath={BlobPath}, BlobExists={BlobExists}",
            documentId,
            document.BlobPath,
            blobExists);

        if (!blobExists)
        {
            return DocumentOperationResponse.Fail($"Document file not found in storage at path '{document.BlobPath}'. Upload may have failed.");
        }

        document.IsActive = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = await GetDocumentByIdAsync(documentId, userId, true, null, cancellationToken);
        return DocumentOperationResponse.Ok(dto);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentSummaryDto>> GetDocumentsByClientAsync(
        Guid clientId,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        // Verify client exists and user has access
        var client = await _dbContext.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

        if (client is null)
        {
            return [];
        }

        // Check home scope for caregivers
        if (allowedHomeIds is not null && !allowedHomeIds.Contains(client.HomeId))
        {
            return [];
        }

        var query = _dbContext.Documents
            .Include(d => d.Client)
            .Include(d => d.Home)
            .Include(d => d.Folder)
            .Include(d => d.UploadedBy)
            .Include(d => d.AccessPermissions)
            .Where(d => d.ClientId == clientId && d.IsActive)
            .AsNoTracking();

        var documents = await query
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(d => new DocumentSummaryDto
        {
            Id = d.Id,
            Scope = d.Scope,
            ClientId = d.ClientId,
            ClientName = d.Client is not null ? $"{d.Client.FirstName} {d.Client.LastName}" : null,
            HomeId = d.HomeId,
            HomeName = d.Home?.Name,
            FolderId = d.FolderId,
            FolderName = d.Folder?.Name,
            FileName = d.FileName,
            ContentType = d.ContentType,
            DocumentType = d.DocumentType,
            Description = d.Description,
            FileSizeBytes = d.FileSizeBytes,
            UploadedAt = d.UploadedAt,
            UploadedByName = d.UploadedBy is not null
                ? $"{d.UploadedBy.FirstName} {d.UploadedBy.LastName}"
                : "Unknown",
            HasAccess = isAdmin || d.AccessPermissions.Any(p => p.CaregiverId == userId)
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<DocumentDto?> GetDocumentByIdAsync(
        Guid documentId,
        Guid userId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Client)
            .Include(d => d.Home)
            .Include(d => d.Folder)
            .Include(d => d.UploadedBy)
            .Include(d => d.AccessPermissions)
                .ThenInclude(p => p.Caregiver)
            .Include(d => d.AccessPermissions)
                .ThenInclude(p => p.GrantedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null || !document.IsActive)
        {
            return null;
        }

        // Check home scope for caregivers
        if (allowedHomeIds is not null && document.Client is not null && !allowedHomeIds.Contains(document.Client.HomeId))
        {
            return null;
        }

        return MapToDto(document);
    }

    /// <inheritdoc />
    public async Task<DocumentViewResponse> GetViewSasUrlAsync(
        Guid documentId,
        Guid userId,
        bool isAdmin,
        string? ipAddress,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Client)
            .Include(d => d.AccessPermissions)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.IsActive, cancellationToken);

        if (document is null)
        {
            return DocumentViewResponse.Fail("Document not found.");
        }

        // Check home scope for caregivers
        if (allowedHomeIds is not null && document.Client is not null && !allowedHomeIds.Contains(document.Client.HomeId))
        {
            return DocumentViewResponse.Fail("Access denied.");
        }

        // Check access permission for caregivers
        if (!isAdmin && !document.AccessPermissions.Any(p => p.CaregiverId == userId))
        {
            return DocumentViewResponse.Fail("You do not have permission to view this document.");
        }

        // Generate read SAS URL (5-minute expiry)
        var (sasUrl, expiresAt) = await _blobStorageService.GetReadSasUrlAsync(document.BlobPath);

        // Get user for audit log
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.DocumentSasGenerated,
            userId,
            user?.Email ?? "Unknown",
            "Document",
            documentId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"View SAS generated for document '{document.FileName}'",
            cancellationToken);

        return DocumentViewResponse.Ok(sasUrl, document.FileName, document.ContentType, expiresAt);
    }

    /// <inheritdoc />
    public async Task<DocumentOperationResponse> DeleteDocumentAsync(
        Guid documentId,
        Guid deletedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .Include(d => d.Client)
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            return DocumentOperationResponse.Fail("Document not found.");
        }

        // Soft delete - mark as inactive
        document.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Delete blob from storage
        await _blobStorageService.DeleteBlobAsync(document.BlobPath);

        // Get user for audit log
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == deletedById, cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.DocumentDeleted,
            deletedById,
            user?.Email ?? "Unknown",
            "Document",
            documentId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Document '{document.FileName}' deleted",
            cancellationToken);

        return DocumentOperationResponse.Ok();
    }

    /// <inheritdoc />
    public async Task<DocumentOperationResponse> GrantAccessAsync(
        Guid documentId,
        GrantDocumentAccessRequest request,
        Guid grantedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CaregiverIds.Count == 0)
        {
            return DocumentOperationResponse.Fail("At least one caregiver must be specified.");
        }

        var document = await _dbContext.Documents
            .Include(d => d.Client)
            .Include(d => d.AccessPermissions)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.IsActive, cancellationToken);

        if (document is null)
        {
            return DocumentOperationResponse.Fail("Document not found.");
        }

        // Verify caregivers are assigned to the same home as the client
        var clientHomeId = document.Client?.HomeId;
        if (clientHomeId is null)
        {
            return DocumentOperationResponse.Fail("Client home not found.");
        }

        var validCaregiverIds = await _dbContext.CaregiverHomeAssignments
            .Where(ca => ca.HomeId == clientHomeId && request.CaregiverIds.Contains(ca.UserId) && ca.IsActive)
            .Select(ca => ca.UserId)
            .ToListAsync(cancellationToken);

        if (validCaregiverIds.Count == 0)
        {
            return DocumentOperationResponse.Fail("None of the specified caregivers are assigned to the client's home.");
        }

        // Add permissions for valid caregivers who don't already have access
        var existingPermissions = document.AccessPermissions.Select(p => p.CaregiverId).ToHashSet();
        var newPermissions = validCaregiverIds
            .Where(id => !existingPermissions.Contains(id))
            .Select(caregiverId => new DocumentAccessPermission
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                CaregiverId = caregiverId,
                GrantedById = grantedById,
                GrantedDate = DateTime.UtcNow
            })
            .ToList();

        if (newPermissions.Count > 0)
        {
            _dbContext.DocumentAccessPermissions.AddRange(newPermissions);

            // Log history entries for each grant
            var historyEntries = newPermissions.Select(p => new DocumentAccessHistory
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                CaregiverId = p.CaregiverId,
                Action = "Granted",
                PerformedById = grantedById,
                PerformedAt = DateTime.UtcNow
            }).ToList();
            _dbContext.DocumentAccessHistory.AddRange(historyEntries);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Get user for audit log
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == grantedById, cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.DocumentAccessGranted,
            grantedById,
            user?.Email ?? "Unknown",
            "Document",
            documentId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Access granted to {newPermissions.Count} caregiver(s) for document '{document.FileName}'",
            cancellationToken);

        var dto = await GetDocumentByIdAsync(documentId, grantedById, true, null, cancellationToken);
        return DocumentOperationResponse.Ok(dto);
    }

    /// <inheritdoc />
    public async Task<DocumentOperationResponse> RevokeAccessAsync(
        Guid documentId,
        Guid caregiverId,
        Guid revokedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var permission = await _dbContext.DocumentAccessPermissions
            .Include(p => p.Document)
            .FirstOrDefaultAsync(p => p.DocumentId == documentId && p.CaregiverId == caregiverId, cancellationToken);

        if (permission is null)
        {
            return DocumentOperationResponse.Fail("Permission not found.");
        }

        _dbContext.DocumentAccessPermissions.Remove(permission);

        // Log history entry for revocation
        var historyEntry = new DocumentAccessHistory
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            CaregiverId = caregiverId,
            Action = "Revoked",
            PerformedById = revokedById,
            PerformedAt = DateTime.UtcNow
        };
        _dbContext.DocumentAccessHistory.Add(historyEntry);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user for audit log
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == revokedById, cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.DocumentAccessRevoked,
            revokedById,
            user?.Email ?? "Unknown",
            "Document",
            documentId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Access revoked from caregiver {caregiverId} for document '{permission.Document?.FileName}'",
            cancellationToken);

        var dto = await GetDocumentByIdAsync(documentId, revokedById, true, null, cancellationToken);
        return DocumentOperationResponse.Ok(dto);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentAccessHistoryDto>> GetAccessHistoryAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var history = await _dbContext.DocumentAccessHistory
            .Include(h => h.Caregiver)
            .Include(h => h.PerformedBy)
            .Where(h => h.DocumentId == documentId)
            .OrderByDescending(h => h.PerformedAt)
            .Select(h => new DocumentAccessHistoryDto
            {
                Id = h.Id,
                DocumentId = h.DocumentId,
                CaregiverId = h.CaregiverId,
                CaregiverName = h.Caregiver != null
                    ? $"{h.Caregiver.FirstName} {h.Caregiver.LastName}"
                    : "Unknown",
                CaregiverEmail = h.Caregiver != null ? h.Caregiver.Email ?? "" : "",
                Action = h.Action,
                PerformedById = h.PerformedById,
                PerformedByName = h.PerformedBy != null
                    ? $"{h.PerformedBy.FirstName} {h.PerformedBy.LastName}"
                    : "Unknown",
                PerformedAt = h.PerformedAt
            })
            .ToListAsync(cancellationToken);

        return history;
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Select(c => invalidChars.Contains(c) ? '_' : c)
            .ToArray());

        // Ensure filename is not too long
        const int maxLength = 200;
        if (sanitized.Length > maxLength)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExt[..(maxLength - extension.Length)] + extension;
        }

        return sanitized;
    }

    /// <summary>
    /// Generates a unique blob filename: {name}_{type}_{checksum}.{ext}
    /// Example: care_plan_CarePlan_A7B3.pdf
    /// </summary>
    private static string GenerateBlobFileName(string originalFileName, Domain.Enums.DocumentType documentType, Guid documentId)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);

        // Sanitize name: replace spaces/dashes with underscores, remove invalid chars
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitizedName = new string(nameWithoutExt
            .Replace(' ', '_')
            .Replace('-', '_')
            .Select(c => invalidChars.Contains(c) ? '_' : c)
            .ToArray());

        // Remove consecutive underscores
        while (sanitizedName.Contains("__"))
        {
            sanitizedName = sanitizedName.Replace("__", "_");
        }

        // Trim underscores from start/end
        sanitizedName = sanitizedName.Trim('_');

        // Limit name length
        if (sanitizedName.Length > 50)
        {
            sanitizedName = sanitizedName[..50];
        }

        // Generate short checksum from document ID (first 4 chars of GUID)
        var checksum = documentId.ToString("N")[..4].ToUpperInvariant();

        return $"{sanitizedName}_{documentType}_{checksum}{extension}";
    }

    private static DocumentDto MapToDto(Document document)
    {
        return new DocumentDto
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
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            DocumentType = document.DocumentType,
            Description = document.Description,
            FileSizeBytes = document.FileSizeBytes,
            UploadedAt = document.UploadedAt,
            UploadedById = document.UploadedById,
            UploadedByName = document.UploadedBy is not null
                ? $"{document.UploadedBy.FirstName} {document.UploadedBy.LastName}"
                : "Unknown",
            IsActive = document.IsActive,
            AccessPermissions = document.AccessPermissions
                .Select(p => new DocumentPermissionDto
                {
                    Id = p.Id,
                    CaregiverId = p.CaregiverId,
                    CaregiverName = p.Caregiver is not null
                        ? $"{p.Caregiver.FirstName} {p.Caregiver.LastName}"
                        : "Unknown",
                    CaregiverEmail = p.Caregiver?.Email ?? "Unknown",
                    GrantedById = p.GrantedById,
                    GrantedByName = p.GrantedBy is not null
                        ? $"{p.GrantedBy.FirstName} {p.GrantedBy.LastName}"
                        : "Unknown",
                    GrantedDate = p.GrantedDate
                })
                .ToList()
        };
    }

    /// <summary>
    /// Generates a blob path based on the document scope.
    /// </summary>
    private static string GenerateBlobPath(
        Domain.Enums.DocumentScope scope,
        Guid? clientId,
        Guid? homeId,
        string blobFileName)
    {
        return scope switch
        {
            Domain.Enums.DocumentScope.Client => $"clients/{clientId}/{blobFileName}",
            Domain.Enums.DocumentScope.Home => $"homes/{homeId}/{blobFileName}",
            Domain.Enums.DocumentScope.Business => $"business/{blobFileName}",
            Domain.Enums.DocumentScope.General => $"general/{blobFileName}",
            _ => $"general/{blobFileName}"
        };
    }
}
