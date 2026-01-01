using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Models.CareLog;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
///     Service implementation for ADL log operations.
/// </summary>
public sealed class ADLLogService : IADLLogService
{
    private readonly IAuditLogService _auditService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ADLLogService> _logger;

    public ADLLogService(
        ApplicationDbContext dbContext,
        IAuditLogService auditService,
        ILogger<ADLLogService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ADLLogOperationResponse> CreateADLLogAsync(
        Guid clientId,
        CreateADLLogRequest request,
        Guid caregiverId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate at least one ADL category is filled
        if (!request.Bathing.HasValue &&
            !request.Dressing.HasValue &&
            !request.Toileting.HasValue &&
            !request.Transferring.HasValue &&
            !request.Continence.HasValue &&
            !request.Feeding.HasValue)
            return new ADLLogOperationResponse
            {
                Success = false,
                Error = "At least one ADL category must be filled."
            };

        // Verify client exists
        var client = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

        if (client is null)
            return new ADLLogOperationResponse
            {
                Success = false,
                Error = "Client not found."
            };

        var adlLog = new ADLLog
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CaregiverId = caregiverId,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            Bathing = request.Bathing,
            Dressing = request.Dressing,
            Toileting = request.Toileting,
            Transferring = request.Transferring,
            Continence = request.Continence,
            Feeding = request.Feeding,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ADLLogs.Add(adlLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get caregiver name for response
        var caregiverName = await GetUserNameAsync(caregiverId, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.ADLLogged,
            caregiverId,
            caregiverName,
            "ADLLog",
            adlLog.Id.ToString(),
            "Success",
            ipAddress,
            $"Logged ADL for client '{client.FirstName} {client.LastName}', Katz Score: {adlLog.CalculateKatzScore()}",
            cancellationToken);

        _logger.LogInformation(
            "ADL logged for client {ClientId} by caregiver {CaregiverId}",
            clientId, caregiverId);

        return new ADLLogOperationResponse
        {
            Success = true,
            ADLLog = MapToDto(adlLog, caregiverName)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ADLLogDto>> GetADLLogsAsync(
        Guid clientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ADLLogs
            .AsNoTracking()
            .Include(a => a.Caregiver)
            .Where(a => a.ClientId == clientId);

        if (fromDate.HasValue) query = query.Where(a => a.Timestamp >= fromDate.Value);

        if (toDate.HasValue) query = query.Where(a => a.Timestamp <= toDate.Value);

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);

        return logs.Select(l => MapToDto(l, GetCaregiverName(l.Caregiver))).ToList();
    }

    /// <inheritdoc />
    public async Task<ADLLogDto?> GetADLLogByIdAsync(
        Guid clientId,
        Guid adlId,
        CancellationToken cancellationToken = default)
    {
        var log = await _dbContext.ADLLogs
            .AsNoTracking()
            .Include(a => a.Caregiver)
            .FirstOrDefaultAsync(a => a.Id == adlId && a.ClientId == clientId, cancellationToken);

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

    private static ADLLogDto MapToDto(ADLLog log, string caregiverName)
    {
        return new ADLLogDto
        {
            Id = log.Id,
            ClientId = log.ClientId,
            CaregiverId = log.CaregiverId,
            CaregiverName = caregiverName,
            Timestamp = log.Timestamp,
            Bathing = log.Bathing,
            Dressing = log.Dressing,
            Toileting = log.Toileting,
            Transferring = log.Transferring,
            Continence = log.Continence,
            Feeding = log.Feeding,
            Notes = log.Notes,
            KatzScore = log.CalculateKatzScore(),
            CreatedAt = log.CreatedAt
        };
    }
}