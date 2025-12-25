using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Models.CareLog;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
/// Service implementation for ROM log operations.
/// </summary>
public sealed class ROMLogService : IROMLogService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditService;
    private readonly ILogger<ROMLogService> _logger;

    public ROMLogService(
        ApplicationDbContext dbContext,
        IAuditLogService auditService,
        ILogger<ROMLogService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ROMLogOperationResponse> CreateROMLogAsync(
        Guid clientId,
        CreateROMLogRequest request,
        Guid caregiverId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ActivityDescription))
        {
            return new ROMLogOperationResponse
            {
                Success = false,
                Error = "Activity description is required."
            };
        }

        // Verify client exists
        var client = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

        if (client is null)
        {
            return new ROMLogOperationResponse
            {
                Success = false,
                Error = "Client not found."
            };
        }

        var romLog = new ROMLog
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CaregiverId = caregiverId,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            ActivityDescription = request.ActivityDescription,
            Duration = request.Duration,
            Repetitions = request.Repetitions,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ROMLogs.Add(romLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var caregiverName = await GetUserNameAsync(caregiverId, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.ROMLogged,
            caregiverId,
            caregiverName,
            "ROMLog",
            romLog.Id.ToString(),
            "Success",
            ipAddress,
            $"Logged ROM exercise '{request.ActivityDescription}' for client '{client.FirstName} {client.LastName}'",
            cancellationToken);

        _logger.LogInformation(
            "ROM logged for client {ClientId} by caregiver {CaregiverId}",
            clientId, caregiverId);

        return new ROMLogOperationResponse
        {
            Success = true,
            ROMLog = MapToDto(romLog, caregiverName)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ROMLogDto>> GetROMLogsAsync(
        Guid clientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ROMLogs
            .AsNoTracking()
            .Include(r => r.Caregiver)
            .Where(r => r.ClientId == clientId);

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.Timestamp <= toDate.Value);
        }

        var logs = await query
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(cancellationToken);

        return logs.Select(l => MapToDto(l, GetCaregiverName(l.Caregiver))).ToList();
    }

    /// <inheritdoc />
    public async Task<ROMLogDto?> GetROMLogByIdAsync(
        Guid clientId,
        Guid romId,
        CancellationToken cancellationToken = default)
    {
        var log = await _dbContext.ROMLogs
            .AsNoTracking()
            .Include(r => r.Caregiver)
            .FirstOrDefaultAsync(r => r.Id == romId && r.ClientId == clientId, cancellationToken);

        return log is null ? null : MapToDto(log, GetCaregiverName(log.Caregiver));
    }

    private async Task<string> GetUserNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is not null ? $"{user.FirstName} {user.LastName}" : "Unknown";
    }

    private static string GetCaregiverName(ApplicationUser? user)
    {
        return user is not null ? $"{user.FirstName} {user.LastName}" : "Unknown";
    }

    private static ROMLogDto MapToDto(ROMLog log, string caregiverName)
    {
        return new ROMLogDto
        {
            Id = log.Id,
            ClientId = log.ClientId,
            CaregiverId = log.CaregiverId,
            CaregiverName = caregiverName,
            Timestamp = log.Timestamp,
            ActivityDescription = log.ActivityDescription,
            Duration = log.Duration,
            Repetitions = log.Repetitions,
            Notes = log.Notes,
            CreatedAt = log.CreatedAt
        };
    }
}
