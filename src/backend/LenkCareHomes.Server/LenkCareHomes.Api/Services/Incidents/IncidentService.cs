using System.Text;
using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Incidents;
using LenkCareHomes.Api.Services.Audit;
using LenkCareHomes.Api.Services.Documents;
using Microsoft.EntityFrameworkCore;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Document = QuestPDF.Fluent.Document;

namespace LenkCareHomes.Api.Services.Incidents;

/// <summary>
///     Service for incident reporting operations.
/// </summary>
public sealed class IncidentService : IIncidentService
{
    private const long MaxPhotoSizeBytes = 10 * 1024 * 1024; // 10MB

    private static readonly string[] AllowedImageTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/heic",
        "image/heif"
    ];

    private readonly IAuditLogService _auditLogService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<IncidentService> _logger;
    private readonly IIncidentNotificationService _notificationService;

    public IncidentService(
        ApplicationDbContext dbContext,
        IAuditLogService auditLogService,
        IIncidentNotificationService notificationService,
        IBlobStorageService blobStorageService,
        ILogger<IncidentService> logger)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IncidentOperationResponse> CreateIncidentAsync(
        CreateIncidentRequest request,
        Guid reportedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Location)) return IncidentOperationResponse.Fail("Location is required.");

        if (string.IsNullOrWhiteSpace(request.Description))
            return IncidentOperationResponse.Fail("Description is required.");

        // Validate severity is between 1-5
        if (request.Severity < 1 || request.Severity > 5)
            return IncidentOperationResponse.Fail("Severity must be between 1 and 5.");

        // Verify client exists if ClientId is provided
        Client? client = null;
        if (request.ClientId.HasValue)
        {
            client = await _dbContext.Clients
                .Include(c => c.Home)
                .FirstOrDefaultAsync(c => c.Id == request.ClientId.Value && c.IsActive, cancellationToken);

            if (client is null) return IncidentOperationResponse.Fail("Client not found or inactive.");
        }

        // Verify home exists
        var home = await _dbContext.Homes
            .FirstOrDefaultAsync(h => h.Id == request.HomeId && h.IsActive, cancellationToken);

        if (home is null) return IncidentOperationResponse.Fail("Home not found or inactive.");

        // Generate incident number with checksum (e.g., IR000004Q)
        var incidentNumber = await GenerateIncidentNumberAsync(home, request.IncidentType, cancellationToken);

        // Create incident
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentNumber = incidentNumber,
            ClientId = request.ClientId,
            HomeId = request.HomeId,
            ReportedById = reportedById,
            IncidentType = request.IncidentType,
            Severity = request.Severity,
            OccurredAt = request.OccurredAt,
            Location = request.Location,
            Description = request.Description,
            ActionsTaken = request.ActionsTaken,
            WitnessNames = request.WitnessNames,
            NotifiedParties = request.NotifiedParties,
            Status = request.SubmitImmediately ? IncidentStatus.Submitted : IncidentStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Incidents.Add(incident);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get reporter name for the DTO
        var reportedBy = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == reportedById, cancellationToken);

        // Determine client name for audit log
        var clientName = client?.FullName ?? "Home-level incident";

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.IncidentCreated,
            reportedById,
            reportedBy?.Email ?? "Unknown",
            "Incident",
            incident.Id.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Incident {incidentNumber} {(request.SubmitImmediately ? "submitted" : "saved as draft")}{(client is not null ? $" for client {client.FullName}" : " (home-level)")}. Type: {request.IncidentType}, Severity: {request.Severity}",
            cancellationToken);

        // Only notify admins when incident is submitted (not for drafts)
        if (request.SubmitImmediately)
        {
            await _notificationService.NotifyAdminsOfNewIncidentAsync(
                incident.Id,
                clientName,
                home.Name,
                request.IncidentType,
                reportedBy?.FullName ?? "Unknown",
                cancellationToken);

            // Update admin notification timestamp
            incident.AdminNotifiedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var dto = await GetIncidentByIdAsync(incident.Id, reportedById, true, null, cancellationToken);
        return IncidentOperationResponse.Ok(dto!);
    }

    /// <inheritdoc />
    public async Task<PagedIncidentResponse> GetIncidentsAsync(
        Guid currentUserId,
        bool isAdmin,
        Guid? homeId = null,
        IncidentStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        IncidentType? incidentType = null,
        Guid? clientId = null,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Incidents
            .Include(i => i.Client)
            .Include(i => i.Home)
            .Include(i => i.ReportedBy)
            .AsNoTracking();

        // Drafts are only visible to the author - even admins cannot see other users' drafts
        // Only submitted/reviewed/closed incidents are visible to others
        query = query.Where(i => i.Status != IncidentStatus.Draft || i.ReportedById == currentUserId);

        // Apply home scope for caregivers
        if (allowedHomeIds is not null) query = query.Where(i => allowedHomeIds.Contains(i.HomeId));

        if (homeId.HasValue) query = query.Where(i => i.HomeId == homeId.Value);

        if (status.HasValue) query = query.Where(i => i.Status == status.Value);

        if (startDate.HasValue) query = query.Where(i => i.OccurredAt >= startDate.Value);

        if (endDate.HasValue) query = query.Where(i => i.OccurredAt <= endDate.Value);

        if (incidentType.HasValue) query = query.Where(i => i.IncidentType == incidentType.Value);

        if (clientId.HasValue) query = query.Where(i => i.ClientId == clientId.Value);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        var incidents = await query
            .OrderByDescending(i => i.OccurredAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new IncidentSummaryDto
            {
                Id = i.Id,
                IncidentNumber = i.IncidentNumber,
                ClientId = i.ClientId,
                ClientName = i.Client != null
                    ? $"{i.Client.FirstName} {i.Client.LastName}"
                    : i.ClientId.HasValue
                        ? "Unknown"
                        : "Home-level Incident",
                HomeId = i.HomeId,
                HomeName = i.Home != null ? i.Home.Name : "Unknown",
                IncidentType = i.IncidentType,
                Severity = i.Severity,
                OccurredAt = i.OccurredAt,
                Status = i.Status,
                ReportedByName = i.ReportedBy != null ? $"{i.ReportedBy.FirstName} {i.ReportedBy.LastName}" : "Unknown",
                CreatedAt = i.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedIncidentResponse.Create(incidents, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<IncidentDto?> GetIncidentByIdAsync(
        Guid incidentId,
        Guid currentUserId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Incidents
            .Include(i => i.Client)
            .Include(i => i.Home)
            .Include(i => i.ReportedBy)
            .Include(i => i.ClosedBy)
            .Include(i => i.FollowUps)
            .ThenInclude(f => f.CreatedBy)
            .Include(i => i.Photos)
            .ThenInclude(p => p.CreatedBy)
            .AsNoTracking();

        // Apply home scope for caregivers
        if (allowedHomeIds is not null) query = query.Where(i => allowedHomeIds.Contains(i.HomeId));

        var incident = await query.FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

        if (incident is null) return null;

        // Drafts are only visible to the author - even admins cannot see other users' drafts
        if (incident.Status == IncidentStatus.Draft && incident.ReportedById != currentUserId) return null;

        return MapToDto(incident);
    }

    /// <inheritdoc />
    public async Task<IncidentOperationResponse> UpdateIncidentAsync(
        Guid incidentId,
        UpdateIncidentRequest request,
        Guid currentUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var incident = await _dbContext.Incidents
            .Include(i => i.Client)
            .Include(i => i.Home)
            .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

        if (incident is null) return IncidentOperationResponse.Fail("Incident not found.");

        // Only the creator can edit their own draft
        if (incident.ReportedById != currentUserId)
            return IncidentOperationResponse.Fail("You can only edit your own incidents.");

        // Can only edit drafts
        if (incident.Status != IncidentStatus.Draft)
            return IncidentOperationResponse.Fail("Only draft incidents can be edited.");

        // Update fields if provided
        if (request.IncidentType.HasValue) incident.IncidentType = request.IncidentType.Value;

        if (request.Severity.HasValue)
        {
            if (request.Severity < 1 || request.Severity > 5)
                return IncidentOperationResponse.Fail("Severity must be between 1 and 5.");
            incident.Severity = request.Severity.Value;
        }

        if (request.OccurredAt.HasValue) incident.OccurredAt = request.OccurredAt.Value;

        if (!string.IsNullOrWhiteSpace(request.Location)) incident.Location = request.Location;

        if (!string.IsNullOrWhiteSpace(request.Description)) incident.Description = request.Description;

        if (request.ActionsTaken is not null) incident.ActionsTaken = request.ActionsTaken;

        if (request.WitnessNames is not null) incident.WitnessNames = request.WitnessNames;

        if (request.NotifiedParties is not null) incident.NotifiedParties = request.NotifiedParties;

        incident.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user for audit log
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.IncidentUpdated,
            currentUserId,
            user?.Email ?? "Unknown",
            "Incident",
            incidentId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Draft incident {incident.IncidentNumber} updated",
            cancellationToken);

        var dto = await GetIncidentByIdAsync(incidentId, currentUserId, true, null, cancellationToken);
        return IncidentOperationResponse.Ok(dto!);
    }

    /// <inheritdoc />
    public async Task<IncidentOperationResponse> SubmitIncidentAsync(
        Guid incidentId,
        Guid currentUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.Incidents
            .Include(i => i.Client)
            .Include(i => i.Home)
            .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

        if (incident is null) return IncidentOperationResponse.Fail("Incident not found.");

        // Only the creator can submit their own draft
        if (incident.ReportedById != currentUserId)
            return IncidentOperationResponse.Fail("You can only submit your own incidents.");

        // Can only submit drafts
        if (incident.Status != IncidentStatus.Draft)
            return IncidentOperationResponse.Fail("Only draft incidents can be submitted.");

        incident.Status = IncidentStatus.Submitted;
        incident.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user for audit/notification
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        // Notify admins now that the incident is submitted
        var clientName = incident.Client?.FullName ?? "Home-level incident";
        await _notificationService.NotifyAdminsOfNewIncidentAsync(
            incident.Id,
            clientName,
            incident.Home?.Name ?? "Unknown",
            incident.IncidentType,
            user?.FullName ?? "Unknown",
            cancellationToken);

        incident.AdminNotifiedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.IncidentSubmitted,
            currentUserId,
            user?.Email ?? "Unknown",
            "Incident",
            incidentId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Incident {incident.IncidentNumber} submitted for review",
            cancellationToken);

        var dto = await GetIncidentByIdAsync(incidentId, currentUserId, true, null, cancellationToken);
        return IncidentOperationResponse.Ok(dto!);
    }

    /// <inheritdoc />
    public async Task<IncidentOperationResponse> UpdateIncidentStatusAsync(
        Guid incidentId,
        UpdateIncidentStatusRequest request,
        Guid closedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var incident = await _dbContext.Incidents
            .Include(i => i.Client)
            .Include(i => i.Home)
            .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

        if (incident is null) return IncidentOperationResponse.Fail("Incident not found.");

        // Require closure notes when closing an incident
        if (request.NewStatus == IncidentStatus.Closed && string.IsNullOrWhiteSpace(request.ClosureNotes))
            return IncidentOperationResponse.Fail("Closure notes are required when closing an incident.");

        var oldStatus = incident.Status;
        incident.Status = request.NewStatus;
        incident.UpdatedAt = DateTime.UtcNow;

        // If closing the incident, set closure fields
        if (request.NewStatus == IncidentStatus.Closed)
        {
            incident.ClosedById = closedById;
            incident.ClosedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.ClosureNotes)) incident.ClosureNotes = request.ClosureNotes;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user for audit log
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == closedById, cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.IncidentStatusUpdated,
            closedById,
            user?.Email ?? "Unknown",
            "Incident",
            incidentId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Incident {incident.IncidentNumber} status changed from {oldStatus} to {request.NewStatus}",
            cancellationToken);

        var dto = await GetIncidentByIdAsync(incidentId, closedById, true, null, cancellationToken);
        return IncidentOperationResponse.Ok(dto!);
    }

    /// <inheritdoc />
    public async Task<IncidentOperationResponse> AddFollowUpAsync(
        Guid incidentId,
        AddIncidentFollowUpRequest request,
        Guid createdById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Note))
            return IncidentOperationResponse.Fail("Follow-up note is required.");

        var incident = await _dbContext.Incidents
            .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

        if (incident is null) return IncidentOperationResponse.Fail("Incident not found.");

        var followUp = new IncidentFollowUp
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            CreatedById = createdById,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.IncidentFollowUps.Add(followUp);
        incident.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user for audit log
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == createdById, cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.IncidentFollowUpAdded,
            createdById,
            user?.Email ?? "Unknown",
            "Incident",
            incidentId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Follow-up note added to incident {incident.IncidentNumber}",
            cancellationToken);

        var dto = await GetIncidentByIdAsync(incidentId, createdById, true, null, cancellationToken);
        return IncidentOperationResponse.Ok(dto!);
    }

    /// <inheritdoc />
    public async Task<IncidentOperationResponse> DeleteIncidentAsync(
        Guid incidentId,
        Guid currentUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.Incidents
            .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

        if (incident is null) return IncidentOperationResponse.Fail("Incident not found.");

        // Only the creator can delete their own draft
        if (incident.ReportedById != currentUserId)
            return IncidentOperationResponse.Fail("You can only delete your own incidents.");

        // Can only delete drafts
        if (incident.Status != IncidentStatus.Draft)
            return IncidentOperationResponse.Fail("Only draft incidents can be deleted.");

        var incidentNumber = incident.IncidentNumber;

        _dbContext.Incidents.Remove(incident);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user for audit log
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.IncidentDeleted,
            currentUserId,
            user?.Email ?? "Unknown",
            "Incident",
            incidentId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Draft incident {incidentNumber} deleted",
            cancellationToken);

        return new IncidentOperationResponse { Success = true };
    }

    /// <inheritdoc />
    public async Task<int> GetRecentNewIncidentsCountAsync(
        int days = 7,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        // Count incidents that are not yet closed
        return await _dbContext.Incidents
            .Where(i => i.Status != IncidentStatus.Closed && i.CreatedAt >= cutoffDate)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IncidentPhotoUploadResponse> InitiatePhotoUploadAsync(
        Guid incidentId,
        UploadIncidentPhotoRequest request,
        Guid uploadedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate file type
        if (!AllowedImageTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
            return IncidentPhotoUploadResponse.Fail(
                $"Invalid file type. Allowed types: {string.Join(", ", AllowedImageTypes)}");

        // Validate file size
        if (request.FileSizeBytes > MaxPhotoSizeBytes)
            return IncidentPhotoUploadResponse.Fail(
                $"File too large. Maximum size is {MaxPhotoSizeBytes / (1024 * 1024)}MB.");

        // Verify incident exists
        var incident = await _dbContext.Incidents
            .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

        if (incident is null) return IncidentPhotoUploadResponse.Fail("Incident not found.");

        // Get current photo count for display order
        var photoCount = await _dbContext.IncidentPhotos
            .Where(p => p.IncidentId == incidentId)
            .CountAsync(cancellationToken);

        // Create photo record
        var photoId = Guid.NewGuid();
        var fileExtension = Path.GetExtension(request.FileName);
        // Path within incident-photos container: {homeId}/{incidentId}/{photoId}.{ext}
        var blobPath = $"{incident.HomeId}/{incidentId}/{photoId}{fileExtension}";

        var photo = new IncidentPhoto
        {
            Id = photoId,
            IncidentId = incidentId,
            BlobPath = blobPath,
            FileName = request.FileName,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            DisplayOrder = photoCount,
            Caption = request.Caption,
            CreatedById = uploadedById,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.IncidentPhotos.Add(photo);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Generate upload SAS URL
        var (uploadUrl, expiresAt) = await _blobStorageService.GetUploadSasUrlAsync(
            blobPath,
            request.ContentType,
            10, // 10 minutes to upload
            BlobContainers.IncidentPhotos);

        _logger.LogInformation(
            "Initiated photo upload for incident {IncidentId}, photo {PhotoId}",
            incidentId,
            photoId);

        return IncidentPhotoUploadResponse.Ok(photoId, uploadUrl, expiresAt);
    }

    /// <inheritdoc />
    public async Task<IncidentPhotoOperationResponse> ConfirmPhotoUploadAsync(
        Guid photoId,
        Guid currentUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var photo = await _dbContext.IncidentPhotos
            .Include(p => p.CreatedBy)
            .Include(p => p.Incident)
            .FirstOrDefaultAsync(p => p.Id == photoId, cancellationToken);

        if (photo is null) return IncidentPhotoOperationResponse.Fail("Photo not found.");

        // Verify blob exists
        var blobExists = await _blobStorageService.BlobExistsAsync(photo.BlobPath, BlobContainers.IncidentPhotos);
        if (!blobExists)
        {
            // Remove the pending photo record
            _dbContext.IncidentPhotos.Remove(photo);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return IncidentPhotoOperationResponse.Fail("Photo upload failed. Please try again.");
        }

        // Get user for audit
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.DocumentUploaded,
            currentUserId,
            user?.Email ?? "Unknown",
            "IncidentPhoto",
            photoId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Photo uploaded for incident {photo.Incident?.IncidentNumber}",
            cancellationToken);

        _logger.LogInformation(
            "Confirmed photo upload for incident {IncidentId}, photo {PhotoId}",
            photo.IncidentId,
            photoId);

        var dto = MapPhotoToDto(photo);
        return IncidentPhotoOperationResponse.Ok(dto);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IncidentPhotoDto>> GetIncidentPhotosAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var photos = await _dbContext.IncidentPhotos
            .Include(p => p.CreatedBy)
            .Where(p => p.IncidentId == incidentId)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return photos.Select(MapPhotoToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<IncidentPhotoViewResponse> GetPhotoViewUrlAsync(
        Guid photoId,
        Guid currentUserId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds,
        CancellationToken cancellationToken = default)
    {
        var photo = await _dbContext.IncidentPhotos
            .Include(p => p.Incident)
            .FirstOrDefaultAsync(p => p.Id == photoId, cancellationToken);

        if (photo is null) return IncidentPhotoViewResponse.Fail("Photo not found.");

        // Check access to incident
        var incident = photo.Incident;
        if (incident is null) return IncidentPhotoViewResponse.Fail("Incident not found.");

        // Verify home access for caregivers
        if (!isAdmin && allowedHomeIds is not null && !allowedHomeIds.Contains(incident.HomeId))
            return IncidentPhotoViewResponse.Fail("Access denied.");

        // Draft incidents are only visible to the creator
        if (incident.Status == IncidentStatus.Draft && incident.ReportedById != currentUserId && !isAdmin)
            return IncidentPhotoViewResponse.Fail("Access denied.");

        var (url, expiresAt) =
            await _blobStorageService.GetReadSasUrlAsync(photo.BlobPath, 30, BlobContainers.IncidentPhotos);

        return IncidentPhotoViewResponse.Ok(url, expiresAt);
    }

    /// <inheritdoc />
    public async Task<IncidentPhotoOperationResponse> DeletePhotoAsync(
        Guid photoId,
        Guid currentUserId,
        bool isAdmin,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var photo = await _dbContext.IncidentPhotos
            .Include(p => p.Incident)
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.Id == photoId, cancellationToken);

        if (photo is null) return IncidentPhotoOperationResponse.Fail("Photo not found.");

        var incident = photo.Incident;
        if (incident is null) return IncidentPhotoOperationResponse.Fail("Incident not found.");

        // Only author or admin can delete photos
        if (!isAdmin && incident.ReportedById != currentUserId)
            return IncidentPhotoOperationResponse.Fail("Only the incident author or admin can delete photos.");

        // Only allow deletion if incident is still in draft
        if (incident.Status != IncidentStatus.Draft && !isAdmin)
            return IncidentPhotoOperationResponse.Fail("Photos can only be deleted from draft incidents.");

        // Delete blob from incident-photos container
        await _blobStorageService.DeleteBlobAsync(photo.BlobPath, BlobContainers.IncidentPhotos);

        // Remove from database
        _dbContext.IncidentPhotos.Remove(photo);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Re-order remaining photos
        var remainingPhotos = await _dbContext.IncidentPhotos
            .Where(p => p.IncidentId == incident.Id)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < remainingPhotos.Count; i++) remainingPhotos[i].DisplayOrder = i;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user for audit
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        // Log audit event
        await _auditLogService.LogPhiAccessAsync(
            AuditActions.DocumentDeleted,
            currentUserId,
            user?.Email ?? "Unknown",
            "IncidentPhoto",
            photoId.ToString(),
            AuditOutcome.Success,
            ipAddress,
            $"Photo deleted from incident {incident.IncidentNumber}",
            cancellationToken);

        _logger.LogInformation(
            "Deleted photo {PhotoId} from incident {IncidentId}",
            photoId,
            incident.Id);

        return new IncidentPhotoOperationResponse { Success = true };
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportIncidentPdfAsync(
        Guid incidentId,
        Guid currentUserId,
        bool isAdmin,
        IReadOnlyList<Guid>? allowedHomeIds,
        CancellationToken cancellationToken = default)
    {
        // Get full incident with all related data
        var incident = await _dbContext.Incidents
            .Include(i => i.Client)
            .Include(i => i.Home)
            .Include(i => i.ReportedBy)
            .Include(i => i.ClosedBy)
            .Include(i => i.FollowUps)
            .ThenInclude(f => f.CreatedBy)
            .Include(i => i.Photos)
            .ThenInclude(p => p.CreatedBy)
            .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

        if (incident is null) throw new InvalidOperationException("Incident not found.");

        // Verify access
        if (!isAdmin && allowedHomeIds is not null && !allowedHomeIds.Contains(incident.HomeId))
            throw new UnauthorizedAccessException("Access denied.");

        if (incident.Status == IncidentStatus.Draft && incident.ReportedById != currentUserId && !isAdmin)
            throw new UnauthorizedAccessException("Access denied.");

        // Generate PDF using QuestPDF
        var pdf = await GenerateIncidentPdfAsync(incident, cancellationToken);

        // Log audit event
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        await _auditLogService.LogPhiAccessAsync(
            "IncidentReportExported",
            currentUserId,
            user?.Email ?? "Unknown",
            "Incident",
            incidentId.ToString(),
            AuditOutcome.Success,
            null,
            $"Incident report {incident.IncidentNumber} exported as PDF",
            cancellationToken);

        return pdf;
    }

    private async Task<byte[]> GenerateIncidentPdfAsync(Incident incident, CancellationToken cancellationToken)
    {
        // Configure QuestPDF license
        Settings.License = LicenseType.Community;

        // Colors matching the application theme
        const string PrimaryColor = "#2d3732";
        const string AccentColor = "#5a7a6b";
        const string HeaderBgColor = "#f0f4f2";
        const string BorderColor = "#d0d7d4";
        const string ConfidentialityText = "CONFIDENTIAL - Contains Protected Health Information (PHI)";

        // Pre-fetch photo images from blob storage
        var photoImages = new List<(IncidentPhoto Photo, byte[]? ImageData)>();
        foreach (var photo in incident.Photos.OrderBy(p => p.DisplayOrder))
            try
            {
                var imageBytes = await _blobStorageService.DownloadBlobAsync(
                    photo.BlobPath,
                    BlobContainers.IncidentPhotos,
                    cancellationToken);
                photoImages.Add((photo, imageBytes));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download photo {PhotoId} for PDF export", photo.Id);
                photoImages.Add((photo, null));
            }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(PrimaryColor));

                // Header
                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("LenkCare Homes")
                                .FontSize(20)
                                .Bold()
                                .FontColor(AccentColor);
                            col.Item().Text($"Incident Report: {incident.IncidentNumber}")
                                .FontSize(14)
                                .SemiBold();
                        });
                    });
                    column.Item().PaddingTop(5).BorderBottom(1).BorderColor(BorderColor);
                });

                // Content
                page.Content().Column(column =>
                {
                    column.Spacing(15);

                    // Summary Section
                    column.Item().Background(HeaderBgColor).Padding(10).Column(summaryCol =>
                    {
                        summaryCol.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Type: {incident.IncidentType}").SemiBold();
                                col.Item().Text(
                                    $"Severity: {incident.Severity}/5 ({GetSeverityLabel(incident.Severity)})");
                                col.Item().Text($"Status: {incident.Status}");
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Occurred: {incident.OccurredAt:MMM dd, yyyy HH:mm}").SemiBold();
                                col.Item().Text($"Location: {incident.Location}");
                                col.Item().Text($"Reported: {incident.CreatedAt:MMM dd, yyyy HH:mm}");
                            });
                        });
                    });

                    // Client & Home Section
                    column.Item().Border(1).BorderColor(BorderColor).Padding(10).Column(infoCol =>
                    {
                        infoCol.Item().Text("Incident Information").Bold().FontSize(11);
                        infoCol.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(
                                    $"Client: {(incident.Client is not null ? $"{incident.Client.FirstName} {incident.Client.LastName}" : "N/A (Home-level incident)")}");
                                col.Item().Text($"Home: {incident.Home?.Name}");
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(
                                    $"Reported By: {incident.ReportedBy?.FirstName} {incident.ReportedBy?.LastName}");
                            });
                        });
                    });

                    // Description Section
                    column.Item().Column(descCol =>
                    {
                        descCol.Item().Background(AccentColor).Padding(8).Text("Description")
                            .FontSize(11).Bold().FontColor(Colors.White);
                        descCol.Item().Border(1).BorderColor(BorderColor).Padding(10).Text(incident.Description);
                    });

                    // Actions Taken Section
                    if (!string.IsNullOrWhiteSpace(incident.ActionsTaken))
                        column.Item().Column(actionCol =>
                        {
                            actionCol.Item().Background(AccentColor).Padding(8).Text("Actions Taken")
                                .FontSize(11).Bold().FontColor(Colors.White);
                            actionCol.Item().Border(1).BorderColor(BorderColor).Padding(10).Text(incident.ActionsTaken);
                        });

                    // Photos Section
                    if (photoImages.Count > 0)
                        column.Item().Column(photoCol =>
                        {
                            photoCol.Item().Background(AccentColor).Padding(8).Text($"Photos ({photoImages.Count})")
                                .FontSize(11).Bold().FontColor(Colors.White);

                            photoCol.Item().Border(1).BorderColor(BorderColor).Padding(10).Column(imgCol =>
                            {
                                foreach (var (photo, imageData) in photoImages)
                                    imgCol.Item().PaddingBottom(10).Column(singlePhotoCol =>
                                    {
                                        if (imageData is not null)
                                            singlePhotoCol.Item().AlignCenter().MaxWidth(400).Image(imageData)
                                                .FitWidth();
                                        else
                                            singlePhotoCol.Item().AlignCenter()
                                                .Text($"[Image not available: {photo.FileName}]")
                                                .FontColor(Colors.Grey.Medium);
                                        if (!string.IsNullOrWhiteSpace(photo.Caption))
                                            singlePhotoCol.Item().AlignCenter().PaddingTop(5).Text(photo.Caption)
                                                .FontSize(9).Italic();
                                        singlePhotoCol.Item().AlignCenter().Text(photo.FileName).FontSize(8)
                                            .FontColor(Colors.Grey.Darken1);
                                    });
                            });
                        });

                    // Follow-ups Section
                    if (incident.FollowUps.Count > 0)
                        column.Item().Column(followUpCol =>
                        {
                            followUpCol.Item().Background(AccentColor).Padding(8)
                                .Text($"Follow-up Notes ({incident.FollowUps.Count})")
                                .FontSize(11).Bold().FontColor(Colors.White);

                            followUpCol.Item().Border(1).BorderColor(BorderColor).Padding(10).Column(notesCol =>
                            {
                                foreach (var followUp in incident.FollowUps.OrderBy(f => f.CreatedAt))
                                    notesCol.Item().PaddingBottom(8).Column(noteCol =>
                                    {
                                        noteCol.Item()
                                            .Text(
                                                $"{followUp.CreatedAt:MMM dd, yyyy HH:mm} - {followUp.CreatedBy?.FirstName} {followUp.CreatedBy?.LastName}")
                                            .SemiBold().FontSize(9);
                                        noteCol.Item().PaddingTop(2).Text(followUp.Note);
                                    });
                            });
                        });

                    // Closure Section
                    if (incident.Status == IncidentStatus.Closed)
                        column.Item().Column(closeCol =>
                        {
                            closeCol.Item().Background(AccentColor).Padding(8).Text("Closure Information")
                                .FontSize(11).Bold().FontColor(Colors.White);

                            closeCol.Item().Border(1).BorderColor(BorderColor).Padding(10).Column(closureCol =>
                            {
                                closureCol.Item()
                                    .Text($"Closed By: {incident.ClosedBy?.FirstName} {incident.ClosedBy?.LastName}")
                                    .SemiBold();
                                closureCol.Item().Text($"Closed At: {incident.ClosedAt:MMM dd, yyyy HH:mm}");
                                if (!string.IsNullOrWhiteSpace(incident.ClosureNotes))
                                {
                                    closureCol.Item().PaddingTop(5).Text("Closure Notes:").SemiBold();
                                    closureCol.Item().Text(incident.ClosureNotes);
                                }
                            });
                        });
                });

                // Footer
                page.Footer().Column(column =>
                {
                    column.Item().BorderTop(1).BorderColor(BorderColor).PaddingTop(5);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text(ConfidentialityText)
                            .FontSize(8)
                            .FontColor(Colors.Red.Medium);
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm UTC} | Page ")
                                .FontSize(8);
                            text.CurrentPageNumber().FontSize(8);
                            text.Span(" of ").FontSize(8);
                            text.TotalPages().FontSize(8);
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string GetSeverityLabel(int severity)
    {
        return severity switch
        {
            1 => "Minor",
            2 => "Low",
            3 => "Moderate",
            4 => "High",
            5 => "Severe",
            _ => "Unknown"
        };
    }

    private static IncidentPhotoDto MapPhotoToDto(IncidentPhoto photo)
    {
        return new IncidentPhotoDto
        {
            Id = photo.Id,
            IncidentId = photo.IncidentId,
            FileName = photo.FileName,
            ContentType = photo.ContentType,
            FileSizeBytes = photo.FileSizeBytes,
            DisplayOrder = photo.DisplayOrder,
            Caption = photo.Caption,
            CreatedAt = photo.CreatedAt,
            CreatedByName = photo.CreatedBy is not null
                ? $"{photo.CreatedBy.FirstName} {photo.CreatedBy.LastName}"
                : "Unknown"
        };
    }

    /// <summary>
    ///     Generates a professional incident reference number using checksum.
    ///     Format: {T}IR{HH}{NNNN}{C} where:
    ///     T  = Incident type prefix (F=Fall, M=Medication, B=Behavioral, X=Medical, I=Injury, E=Elopement, O=Other)
    ///     IR = Incident Report identifier
    ///     HH = Home code (2 chars, base-36 encoded from home sequence)
    ///     NNNN = 4-digit sequence for this home (base-36)
    ///     C = Checksum character (Luhn mod 36)
    ///     Example: FIR010005K = Fall Incident Report, Home #1, incident #5, checksum K
    /// </summary>
    private async Task<string> GenerateIncidentNumberAsync(
        Home home,
        IncidentType incidentType,
        CancellationToken cancellationToken)
    {
        // Get incident type code first
        var typeCode = GetIncidentTypeCode(incidentType);

        // Get home sequence number (order by creation date)
        var homeSequence = await GetHomeSequenceAsync(home.Id, cancellationToken);
        var homeCode = ToBase36(homeSequence).PadLeft(2, '0').ToUpperInvariant();

        // Get incident count for this specific home
        var homeIncidentCount = await _dbContext.Incidents
            .Where(i => i.HomeId == home.Id)
            .CountAsync(cancellationToken);
        var sequence = ToBase36(homeIncidentCount + 1).PadLeft(4, '0').ToUpperInvariant();

        // Build payload: HomeCode + Sequence (type is prefix, not in checksum payload)
        var payload = $"{homeCode}{sequence}";

        // Calculate Luhn mod 36 checksum over full reference (type + IR + payload)
        var fullPayload = $"{typeCode}IR{payload}";
        var checksum = CalculateLuhnMod36Checksum(fullPayload);

        return $"{typeCode}IR{payload}{checksum}";
    }

    /// <summary>
    ///     Gets the sequence number for a home based on creation order.
    /// </summary>
    private async Task<int> GetHomeSequenceAsync(Guid homeId, CancellationToken cancellationToken)
    {
        var homes = await _dbContext.Homes
            .OrderBy(h => h.CreatedAt)
            .Select(h => h.Id)
            .ToListAsync(cancellationToken);

        var index = homes.IndexOf(homeId);
        return index >= 0 ? index + 1 : 1;
    }

    /// <summary>
    ///     Gets a single character code for incident type.
    /// </summary>
    private static char GetIncidentTypeCode(IncidentType type)
    {
        return type switch
        {
            IncidentType.Fall => 'F',
            IncidentType.Medication => 'M',
            IncidentType.Behavioral => 'B',
            IncidentType.Medical => 'X',
            IncidentType.Injury => 'I',
            IncidentType.Elopement => 'E',
            IncidentType.Other => 'O',
            _ => 'O'
        };
    }

    /// <summary>
    ///     Converts a number to base-36 string (0-9, A-Z).
    /// </summary>
    private static string ToBase36(long value)
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (value == 0) return "0";

        var result = new StringBuilder();
        while (value > 0)
        {
            result.Insert(0, chars[(int)(value % 36)]);
            value /= 36;
        }

        return result.ToString();
    }

    /// <summary>
    ///     Calculates Luhn mod 36 checksum character.
    ///     This is an industry-standard algorithm used for validation.
    /// </summary>
    private static char CalculateLuhnMod36Checksum(string input)
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var sum = 0;
        var factor = 2;

        for (var i = input.Length - 1; i >= 0; i--)
        {
            var codePoint = chars.IndexOf(char.ToUpperInvariant(input[i]));
            var addend = factor * codePoint;

            // Sum digits of addend (in base 36)
            addend = addend / 36 + addend % 36;
            sum += addend;

            // Alternate factor between 1 and 2
            factor = factor == 2 ? 1 : 2;
        }

        var remainder = sum % 36;
        var checkCodePoint = (36 - remainder) % 36;

        return chars[checkCodePoint];
    }

    /// <summary>
    ///     Validates an incident number checksum.
    ///     Format: {T}IR{HH}{NNNN}{C} where T is type code (F, M, B, X, I, E, O)
    ///     Returns true if the checksum is valid.
    /// </summary>
    public static bool ValidateIncidentNumber(string incidentNumber)
    {
        if (string.IsNullOrWhiteSpace(incidentNumber) || incidentNumber.Length < 10)
            return false;

        // Check format: starts with type code, then "IR"
        var validTypeCodes = "FMBXIEO";
        if (!validTypeCodes.Contains(incidentNumber[0]) || incidentNumber[1..3] != "IR")
            return false;

        var payload = incidentNumber[..^1]; // Everything except the checksum
        var providedChecksum = incidentNumber[^1];
        var calculatedChecksum = CalculateLuhnMod36Checksum(payload);

        return char.ToUpperInvariant(providedChecksum) == calculatedChecksum;
    }

    private static IncidentDto MapToDto(Incident incident)
    {
        return new IncidentDto
        {
            Id = incident.Id,
            IncidentNumber = incident.IncidentNumber,
            ClientId = incident.ClientId,
            ClientName = incident.Client is not null
                ? $"{incident.Client.FirstName} {incident.Client.LastName}"
                : null,
            HomeId = incident.HomeId,
            HomeName = incident.Home?.Name ?? "Unknown",
            ReportedById = incident.ReportedById,
            ReportedByName = incident.ReportedBy is not null
                ? $"{incident.ReportedBy.FirstName} {incident.ReportedBy.LastName}"
                : "Unknown",
            IncidentType = incident.IncidentType,
            Severity = incident.Severity,
            OccurredAt = incident.OccurredAt,
            Location = incident.Location,
            Description = incident.Description,
            ActionsTaken = incident.ActionsTaken,
            WitnessNames = incident.WitnessNames,
            NotifiedParties = incident.NotifiedParties,
            AdminNotifiedAt = incident.AdminNotifiedAt,
            Status = incident.Status,
            ClosedById = incident.ClosedById,
            ClosedByName = incident.ClosedBy is not null
                ? $"{incident.ClosedBy.FirstName} {incident.ClosedBy.LastName}"
                : null,
            ClosedAt = incident.ClosedAt,
            ClosureNotes = incident.ClosureNotes,
            CreatedAt = incident.CreatedAt,
            UpdatedAt = incident.UpdatedAt,
            FollowUps = incident.FollowUps
                .OrderBy(f => f.CreatedAt)
                .Select(f => new IncidentFollowUpDto
                {
                    Id = f.Id,
                    CreatedById = f.CreatedById,
                    CreatedByName = f.CreatedBy is not null
                        ? $"{f.CreatedBy.FirstName} {f.CreatedBy.LastName}"
                        : "Unknown",
                    Note = f.Note,
                    CreatedAt = f.CreatedAt
                })
                .ToList(),
            Photos = incident.Photos
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.CreatedAt)
                .Select(p => new IncidentPhotoDto
                {
                    Id = p.Id,
                    IncidentId = p.IncidentId,
                    FileName = p.FileName,
                    ContentType = p.ContentType,
                    FileSizeBytes = p.FileSizeBytes,
                    DisplayOrder = p.DisplayOrder,
                    Caption = p.Caption,
                    CreatedAt = p.CreatedAt,
                    CreatedByName = p.CreatedBy is not null
                        ? $"{p.CreatedBy.FirstName} {p.CreatedBy.LastName}"
                        : "Unknown"
                })
                .ToList()
        };
    }
}