using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Models.Caregivers;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.Caregivers;

/// <summary>
///     Service implementation for caregiver management and home assignment operations.
/// </summary>
public sealed class CaregiverService : ICaregiverService
{
    private readonly IAuditLogService _auditService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CaregiverService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public CaregiverService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditService,
        ILogger<CaregiverService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CaregiverSummaryDto>> GetAllCaregiversAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        // Get all users in the Caregiver role
        var caregiverRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == Roles.Caregiver, cancellationToken);

        if (caregiverRole is null) return [];

        var caregiverUserIds = await _dbContext.UserRoles
            .Where(ur => ur.RoleId == caregiverRole.Id)
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken);

        var query = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.HomeAssignments)
            .Where(u => caregiverUserIds.Contains(u.Id));

        if (!includeInactive) query = query.Where(u => u.IsActive);

        var caregivers = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

        return caregivers.Select(c => new CaregiverSummaryDto
        {
            Id = c.Id,
            Email = c.Email ?? string.Empty,
            FirstName = c.FirstName,
            LastName = c.LastName,
            IsActive = c.IsActive,
            InvitationAccepted = c.InvitationAccepted,
            AssignedHomesCount = c.HomeAssignments.Count(ha => ha.IsActive)
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<CaregiverDto?> GetCaregiverByIdAsync(Guid caregiverId,
        CancellationToken cancellationToken = default)
    {
        var caregiver = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.HomeAssignments)
            .ThenInclude(ha => ha.Home)
            .FirstOrDefaultAsync(u => u.Id == caregiverId, cancellationToken);

        if (caregiver is null) return null;

        // Verify user is a caregiver
        var isCaregiver = await _userManager.IsInRoleAsync(caregiver, Roles.Caregiver);
        if (!isCaregiver) return null;

        return MapToDto(caregiver);
    }

    /// <inheritdoc />
    public async Task<CaregiverOperationResponse> AssignHomesAsync(
        Guid caregiverId,
        AssignHomesRequest request,
        Guid assignedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var caregiver = await _dbContext.Users
            .Include(u => u.HomeAssignments)
            .ThenInclude(ha => ha.Home)
            .FirstOrDefaultAsync(u => u.Id == caregiverId, cancellationToken);

        if (caregiver is null)
            return new CaregiverOperationResponse { Success = false, Error = "Caregiver not found." };

        // Verify user is a caregiver
        var isCaregiver = await _userManager.IsInRoleAsync(caregiver, Roles.Caregiver);
        if (!isCaregiver) return new CaregiverOperationResponse { Success = false, Error = "User is not a caregiver." };

        if (!caregiver.IsActive)
            return new CaregiverOperationResponse
                { Success = false, Error = "Cannot assign homes to an inactive caregiver." };

        // Verify all homes exist and are active
        var homes = await _dbContext.Homes
            .Where(h => request.HomeIds.Contains(h.Id))
            .ToListAsync(cancellationToken);

        if (homes.Count != request.HomeIds.Count)
            return new CaregiverOperationResponse { Success = false, Error = "One or more homes not found." };

        var inactiveHomes = homes.Where(h => !h.IsActive).ToList();
        if (inactiveHomes.Count != 0)
            return new CaregiverOperationResponse
            {
                Success = false,
                Error = $"Cannot assign to inactive homes: {string.Join(", ", inactiveHomes.Select(h => h.Name))}"
            };

        // Get existing active assignments
        var existingActiveAssignments = caregiver.HomeAssignments
            .Where(ha => ha.IsActive)
            .ToList();
        var existingActiveHomeIds = existingActiveAssignments.Select(ha => ha.HomeId).ToHashSet();
        var requestedHomeIds = request.HomeIds.ToHashSet();

        // Track changes for audit logging
        var assignedHomeNames = new List<string>();
        var unassignedHomeNames = new List<string>();

        // Remove assignments that are no longer in the request
        foreach (var existingAssignment in existingActiveAssignments)
            if (!requestedHomeIds.Contains(existingAssignment.HomeId))
            {
                existingAssignment.IsActive = false;
                unassignedHomeNames.Add(existingAssignment.Home?.Name ?? existingAssignment.HomeId.ToString());
            }

        // Add new assignments or reactivate inactive ones
        foreach (var homeId in request.HomeIds)
        {
            var existingAssignment = caregiver.HomeAssignments.FirstOrDefault(ha => ha.HomeId == homeId);

            if (existingAssignment is null)
            {
                // Create new assignment
                var assignment = new CaregiverHomeAssignment
                {
                    Id = Guid.NewGuid(),
                    UserId = caregiverId,
                    HomeId = homeId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedById = assignedById,
                    IsActive = true
                };

                _dbContext.CaregiverHomeAssignments.Add(assignment);
                var home = homes.First(h => h.Id == homeId);
                assignedHomeNames.Add(home.Name);
            }
            else if (!existingAssignment.IsActive)
            {
                // Reactivate previously inactive assignment
                existingAssignment.IsActive = true;
                existingAssignment.AssignedAt = DateTime.UtcNow;
                existingAssignment.AssignedById = assignedById;
                var home = homes.First(h => h.Id == homeId);
                assignedHomeNames.Add(home.Name);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user email for audit
        var userEmail = await GetUserEmailAsync(assignedById, cancellationToken);

        // Log assignments
        if (assignedHomeNames.Count != 0)
        {
            await _auditService.LogPhiAccessAsync(
                AuditActions.CaregiverAssigned,
                assignedById,
                userEmail ?? "Unknown",
                "CaregiverHomeAssignment",
                caregiverId.ToString(),
                "Success",
                ipAddress,
                $"Assigned caregiver '{caregiver.FullName}' to homes: {string.Join(", ", assignedHomeNames)}",
                cancellationToken);

            _logger.LogInformation("Caregiver {CaregiverId} assigned to {Count} homes by user {UserId}",
                caregiverId, assignedHomeNames.Count, assignedById);
        }

        // Log unassignments
        if (unassignedHomeNames.Count != 0)
        {
            await _auditService.LogPhiAccessAsync(
                AuditActions.CaregiverUnassigned,
                assignedById,
                userEmail ?? "Unknown",
                "CaregiverHomeAssignment",
                caregiverId.ToString(),
                "Success",
                ipAddress,
                $"Unassigned caregiver '{caregiver.FullName}' from homes: {string.Join(", ", unassignedHomeNames)}",
                cancellationToken);

            _logger.LogInformation("Caregiver {CaregiverId} unassigned from {Count} homes by user {UserId}",
                caregiverId, unassignedHomeNames.Count, assignedById);
        }

        // Reload to get updated assignments
        var updatedCaregiver = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.HomeAssignments)
            .ThenInclude(ha => ha.Home)
            .FirstOrDefaultAsync(u => u.Id == caregiverId, cancellationToken);

        return new CaregiverOperationResponse
        {
            Success = true,
            Caregiver = updatedCaregiver is null ? null : MapToDto(updatedCaregiver)
        };
    }

    /// <inheritdoc />
    public async Task<CaregiverOperationResponse> RemoveHomeAssignmentAsync(
        Guid caregiverId,
        Guid homeId,
        Guid removedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.CaregiverHomeAssignments
            .Include(ca => ca.Home)
            .Include(ca => ca.User)
            .FirstOrDefaultAsync(ca => ca.UserId == caregiverId && ca.HomeId == homeId, cancellationToken);

        if (assignment is null)
            return new CaregiverOperationResponse { Success = false, Error = "Home assignment not found." };

        assignment.IsActive = false;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get user email for audit
        var userEmail = await GetUserEmailAsync(removedById, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.CaregiverUnassigned,
            removedById,
            userEmail ?? "Unknown",
            "CaregiverHomeAssignment",
            caregiverId.ToString(),
            "Success",
            ipAddress,
            $"Removed caregiver '{assignment.User?.FullName}' from home '{assignment.Home?.Name}'",
            cancellationToken);

        _logger.LogInformation("Caregiver {CaregiverId} removed from home {HomeId} by user {UserId}",
            caregiverId, homeId, removedById);

        // Reload to get updated assignments
        var updatedCaregiver = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.HomeAssignments)
            .ThenInclude(ha => ha.Home)
            .FirstOrDefaultAsync(u => u.Id == caregiverId, cancellationToken);

        return new CaregiverOperationResponse
        {
            Success = true,
            Caregiver = updatedCaregiver is null ? null : MapToDto(updatedCaregiver)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetAssignedHomeIdsAsync(
        Guid caregiverId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CaregiverHomeAssignments
            .AsNoTracking()
            .Where(ca => ca.UserId == caregiverId && ca.IsActive)
            .Select(ca => ca.HomeId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CaregiverSummaryDto>> GetCaregiversByHomeAsync(
        Guid homeId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        // Get all caregiver user IDs assigned to this home
        var caregiverUserIds = await _dbContext.CaregiverHomeAssignments
            .AsNoTracking()
            .Where(cha => cha.HomeId == homeId && cha.IsActive)
            .Select(cha => cha.UserId)
            .ToListAsync(cancellationToken);

        if (caregiverUserIds.Count == 0) return [];

        var query = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.HomeAssignments)
            .Where(u => caregiverUserIds.Contains(u.Id));

        if (!includeInactive) query = query.Where(u => u.IsActive);

        var caregivers = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

        return caregivers.Select(c => new CaregiverSummaryDto
        {
            Id = c.Id,
            Email = c.Email ?? string.Empty,
            FirstName = c.FirstName,
            LastName = c.LastName,
            IsActive = c.IsActive,
            InvitationAccepted = c.InvitationAccepted,
            AssignedHomesCount = c.HomeAssignments.Count(ha => ha.IsActive)
        }).ToList();
    }

    private static CaregiverDto MapToDto(ApplicationUser caregiver)
    {
        return new CaregiverDto
        {
            Id = caregiver.Id,
            Email = caregiver.Email ?? string.Empty,
            FirstName = caregiver.FirstName,
            LastName = caregiver.LastName,
            IsActive = caregiver.IsActive,
            IsMfaSetupComplete = caregiver.IsMfaSetupComplete,
            InvitationAccepted = caregiver.InvitationAccepted,
            CreatedAt = caregiver.CreatedAt,
            UpdatedAt = caregiver.UpdatedAt,
            HomeAssignments = caregiver.HomeAssignments
                .Where(ha => ha.IsActive)
                .Select(ha => new CaregiverHomeAssignmentDto
                {
                    Id = ha.Id,
                    HomeId = ha.HomeId,
                    HomeName = ha.Home?.Name ?? "Unknown",
                    AssignedAt = ha.AssignedAt,
                    IsActive = ha.IsActive
                }).ToList()
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