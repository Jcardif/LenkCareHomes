using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Homes;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.Homes;

/// <summary>
/// Service implementation for home management operations.
/// </summary>
public sealed class HomeService : IHomeService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditService;
    private readonly ILogger<HomeService> _logger;

    public HomeService(
        ApplicationDbContext dbContext,
        IAuditLogService auditService,
        ILogger<HomeService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HomeSummaryDto>> GetAllHomesAsync(
        bool includeInactive = false,
        IReadOnlyList<Guid>? allowedHomeIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Homes.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(h => h.IsActive);
        }

        // Filter by allowed homes (for caregivers)
        if (allowedHomeIds is not null)
        {
            query = query.Where(h => allowedHomeIds.Contains(h.Id));
        }

        var homes = await query
            .Include(h => h.Beds)
            .Include(h => h.Clients)
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);

        return homes.Select(h => new HomeSummaryDto
        {
            Id = h.Id,
            Name = h.Name,
            City = h.City,
            State = h.State,
            IsActive = h.IsActive,
            Capacity = h.Capacity,
            AvailableBeds = h.Beds.Count(b => b.IsActive && b.Status == BedStatus.Available),
            ActiveClients = h.Clients.Count(c => c.IsActive)
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<HomeDto?> GetHomeByIdAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        var home = await _dbContext.Homes
            .AsNoTracking()
            .Include(h => h.Beds)
            .Include(h => h.Clients)
            .FirstOrDefaultAsync(h => h.Id == homeId, cancellationToken);

        if (home is null)
        {
            return null;
        }

        return MapToDto(home);
    }

    /// <inheritdoc />
    public async Task<HomeOperationResponse> CreateHomeAsync(
        CreateHomeRequest request,
        Guid createdById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new HomeOperationResponse { Success = false, Error = "Home name is required." };
        }

        if (request.Capacity <= 0)
        {
            return new HomeOperationResponse { Success = false, Error = "Capacity must be greater than 0." };
        }

        // Check for duplicate name
        var existingHome = await _dbContext.Homes
            .AnyAsync(h => h.Name == request.Name, cancellationToken);

        if (existingHome)
        {
            return new HomeOperationResponse { Success = false, Error = "A home with this name already exists." };
        }

        var home = new Home
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            PhoneNumber = request.PhoneNumber,
            Capacity = request.Capacity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedById = createdById
        };

        _dbContext.Homes.Add(home);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user email for audit
        var userEmail = await GetUserEmailAsync(createdById, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.HomeCreated,
            createdById,
            userEmail ?? "Unknown",
            "Home",
            home.Id.ToString(),
            "Success",
            ipAddress,
            $"Created home: {home.Name}",
            cancellationToken);

        _logger.LogInformation("Home {HomeId} created by user {UserId}", home.Id, createdById);

        return new HomeOperationResponse
        {
            Success = true,
            Home = MapToDto(home)
        };
    }

    /// <inheritdoc />
    public async Task<HomeOperationResponse> UpdateHomeAsync(
        Guid homeId,
        UpdateHomeRequest request,
        Guid updatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var home = await _dbContext.Homes
            .Include(h => h.Beds)
            .Include(h => h.Clients)
            .FirstOrDefaultAsync(h => h.Id == homeId, cancellationToken);

        if (home is null)
        {
            return new HomeOperationResponse { Success = false, Error = "Home not found." };
        }

        // Check for duplicate name if name is being changed
        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != home.Name)
        {
            var existingHome = await _dbContext.Homes
                .AnyAsync(h => h.Name == request.Name && h.Id != homeId, cancellationToken);

            if (existingHome)
            {
                return new HomeOperationResponse { Success = false, Error = "A home with this name already exists." };
            }
        }

        // Update properties if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            home.Name = request.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.Address))
        {
            home.Address = request.Address;
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            home.City = request.City;
        }

        if (!string.IsNullOrWhiteSpace(request.State))
        {
            home.State = request.State;
        }

        if (!string.IsNullOrWhiteSpace(request.ZipCode))
        {
            home.ZipCode = request.ZipCode;
        }

        if (request.PhoneNumber is not null)
        {
            home.PhoneNumber = request.PhoneNumber;
        }

        if (request.Capacity.HasValue)
        {
            if (request.Capacity.Value <= 0)
            {
                return new HomeOperationResponse { Success = false, Error = "Capacity must be greater than 0." };
            }
            home.Capacity = request.Capacity.Value;
        }

        home.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user email for audit
        var userEmail = await GetUserEmailAsync(updatedById, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.HomeUpdated,
            updatedById,
            userEmail ?? "Unknown",
            "Home",
            home.Id.ToString(),
            "Success",
            ipAddress,
            $"Updated home: {home.Name}",
            cancellationToken);

        _logger.LogInformation("Home {HomeId} updated by user {UserId}", home.Id, updatedById);

        return new HomeOperationResponse
        {
            Success = true,
            Home = MapToDto(home)
        };
    }

    /// <inheritdoc />
    public async Task<HomeOperationResponse> DeactivateHomeAsync(
        Guid homeId,
        Guid deactivatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var home = await _dbContext.Homes
            .Include(h => h.Beds)
            .Include(h => h.Clients)
            .FirstOrDefaultAsync(h => h.Id == homeId, cancellationToken);

        if (home is null)
        {
            return new HomeOperationResponse { Success = false, Error = "Home not found." };
        }

        // Check if there are active clients
        var activeClientsCount = home.Clients.Count(c => c.IsActive);
        if (activeClientsCount > 0)
        {
            return new HomeOperationResponse
            {
                Success = false,
                Error = $"Cannot deactivate home with {activeClientsCount} active client(s). Please discharge all clients first."
            };
        }

        home.IsActive = false;
        home.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user email for audit
        var userEmail = await GetUserEmailAsync(deactivatedById, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.HomeDeactivated,
            deactivatedById,
            userEmail ?? "Unknown",
            "Home",
            home.Id.ToString(),
            "Success",
            ipAddress,
            $"Deactivated home: {home.Name}",
            cancellationToken);

        _logger.LogInformation("Home {HomeId} deactivated by user {UserId}", home.Id, deactivatedById);

        return new HomeOperationResponse
        {
            Success = true,
            Home = MapToDto(home)
        };
    }

    /// <inheritdoc />
    public async Task<HomeOperationResponse> ReactivateHomeAsync(
        Guid homeId,
        Guid reactivatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var home = await _dbContext.Homes
            .Include(h => h.Beds)
            .Include(h => h.Clients)
            .FirstOrDefaultAsync(h => h.Id == homeId, cancellationToken);

        if (home is null)
        {
            return new HomeOperationResponse { Success = false, Error = "Home not found." };
        }

        home.IsActive = true;
        home.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user email for audit
        var userEmail = await GetUserEmailAsync(reactivatedById, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.HomeReactivated,
            reactivatedById,
            userEmail ?? "Unknown",
            "Home",
            home.Id.ToString(),
            "Success",
            ipAddress,
            $"Reactivated home: {home.Name}",
            cancellationToken);

        _logger.LogInformation("Home {HomeId} reactivated by user {UserId}", home.Id, reactivatedById);

        return new HomeOperationResponse
        {
            Success = true,
            Home = MapToDto(home)
        };
    }

    private static HomeDto MapToDto(Home home)
    {
        var totalBeds = home.Beds?.Count(b => b.IsActive) ?? 0;
        var availableBeds = home.Beds?.Count(b => b.IsActive && b.Status == BedStatus.Available) ?? 0;
        var occupiedBeds = home.Beds?.Count(b => b.IsActive && b.Status == BedStatus.Occupied) ?? 0;
        var activeClients = home.Clients?.Count(c => c.IsActive) ?? 0;

        return new HomeDto
        {
            Id = home.Id,
            Name = home.Name,
            Address = home.Address,
            City = home.City,
            State = home.State,
            ZipCode = home.ZipCode,
            PhoneNumber = home.PhoneNumber,
            Capacity = home.Capacity,
            IsActive = home.IsActive,
            CreatedAt = home.CreatedAt,
            UpdatedAt = home.UpdatedAt,
            TotalBeds = totalBeds,
            AvailableBeds = availableBeds,
            OccupiedBeds = occupiedBeds,
            ActiveClients = activeClients
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
