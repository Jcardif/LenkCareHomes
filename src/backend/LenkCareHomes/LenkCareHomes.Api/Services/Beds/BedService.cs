using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Beds;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.Beds;

/// <summary>
///     Service implementation for bed management operations.
/// </summary>
public sealed class BedService : IBedService
{
    private readonly IAuditLogService _auditService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BedService> _logger;

    public BedService(
        ApplicationDbContext dbContext,
        IAuditLogService auditService,
        ILogger<BedService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BedDto>> GetBedsByHomeIdAsync(
        Guid homeId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Beds
            .AsNoTracking()
            .Include(b => b.CurrentOccupant)
            .Where(b => b.HomeId == homeId);

        if (!includeInactive) query = query.Where(b => b.IsActive);

        var beds = await query
            .OrderBy(b => b.Label)
            .ToListAsync(cancellationToken);

        return beds.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<BedDto?> GetBedByIdAsync(Guid bedId, CancellationToken cancellationToken = default)
    {
        var bed = await _dbContext.Beds
            .AsNoTracking()
            .Include(b => b.CurrentOccupant)
            .FirstOrDefaultAsync(b => b.Id == bedId, cancellationToken);

        return bed is null ? null : MapToDto(bed);
    }

    /// <inheritdoc />
    public async Task<BedOperationResponse> CreateBedAsync(
        Guid homeId,
        CreateBedRequest request,
        Guid createdById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Label))
            return new BedOperationResponse { Success = false, Error = "Bed label is required." };

        // Verify home exists and is active
        var home = await _dbContext.Homes
            .FirstOrDefaultAsync(h => h.Id == homeId, cancellationToken);

        if (home is null) return new BedOperationResponse { Success = false, Error = "Home not found." };

        if (!home.IsActive)
            return new BedOperationResponse { Success = false, Error = "Cannot add beds to an inactive home." };

        // Check for duplicate label within the home
        var existingBed = await _dbContext.Beds
            .AnyAsync(b => b.HomeId == homeId && b.Label == request.Label, cancellationToken);

        if (existingBed)
            return new BedOperationResponse
            {
                Success = false,
                Error = $"A bed with label '{request.Label}' already exists in this home."
            };

        var bed = new Bed
        {
            Id = Guid.NewGuid(),
            HomeId = homeId,
            Label = request.Label,
            Status = BedStatus.Available,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Beds.Add(bed);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user email for audit
        var userEmail = await GetUserEmailAsync(createdById, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.BedCreated,
            createdById,
            userEmail ?? "Unknown",
            "Bed",
            bed.Id.ToString(),
            "Success",
            ipAddress,
            $"Created bed '{bed.Label}' in home '{home.Name}'",
            cancellationToken);

        _logger.LogInformation("Bed {BedId} created in home {HomeId} by user {UserId}", bed.Id, homeId, createdById);

        return new BedOperationResponse
        {
            Success = true,
            Bed = MapToDto(bed)
        };
    }

    /// <inheritdoc />
    public async Task<BedOperationResponse> UpdateBedAsync(
        Guid bedId,
        UpdateBedRequest request,
        Guid updatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bed = await _dbContext.Beds
            .Include(b => b.CurrentOccupant)
            .Include(b => b.Home)
            .FirstOrDefaultAsync(b => b.Id == bedId, cancellationToken);

        if (bed is null) return new BedOperationResponse { Success = false, Error = "Bed not found." };

        // Check if trying to deactivate an occupied bed
        if (request.IsActive == false && bed.Status == BedStatus.Occupied)
            return new BedOperationResponse
            {
                Success = false,
                Error = "Cannot deactivate an occupied bed. Please discharge the client first."
            };

        // Check for duplicate label if label is being changed
        if (!string.IsNullOrWhiteSpace(request.Label) && request.Label != bed.Label)
        {
            var existingBed = await _dbContext.Beds
                .AnyAsync(b => b.HomeId == bed.HomeId && b.Label == request.Label && b.Id != bedId, cancellationToken);

            if (existingBed)
                return new BedOperationResponse
                {
                    Success = false,
                    Error = $"A bed with label '{request.Label}' already exists in this home."
                };

            bed.Label = request.Label;
        }

        if (request.IsActive.HasValue) bed.IsActive = request.IsActive.Value;

        bed.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user email for audit
        var userEmail = await GetUserEmailAsync(updatedById, cancellationToken);

        var action = request.IsActive == false ? AuditActions.BedDeactivated :
            request.IsActive == true ? AuditActions.BedReactivated :
            AuditActions.BedUpdated;

        await _auditService.LogPhiAccessAsync(
            action,
            updatedById,
            userEmail ?? "Unknown",
            "Bed",
            bed.Id.ToString(),
            "Success",
            ipAddress,
            $"Updated bed '{bed.Label}' in home '{bed.Home?.Name}'",
            cancellationToken);

        _logger.LogInformation("Bed {BedId} updated by user {UserId}", bed.Id, updatedById);

        return new BedOperationResponse
        {
            Success = true,
            Bed = MapToDto(bed)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BedDto>> GetAvailableBedsAsync(
        Guid homeId,
        CancellationToken cancellationToken = default)
    {
        var beds = await _dbContext.Beds
            .AsNoTracking()
            .Where(b => b.HomeId == homeId && b.IsActive && b.Status == BedStatus.Available)
            .OrderBy(b => b.Label)
            .ToListAsync(cancellationToken);

        return beds.Select(MapToDto).ToList();
    }

    private static BedDto MapToDto(Bed bed)
    {
        return new BedDto
        {
            Id = bed.Id,
            HomeId = bed.HomeId,
            Label = bed.Label,
            Status = bed.Status.ToString(),
            IsActive = bed.IsActive,
            CreatedAt = bed.CreatedAt,
            UpdatedAt = bed.UpdatedAt,
            CurrentOccupantId = bed.CurrentOccupant?.Id,
            CurrentOccupantName = bed.CurrentOccupant?.FullName
        };
    }

    private async Task<string?> GetUserEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user?.Email;
    }
}