using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Models.CareLog;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
///     Service implementation for medication log operations.
/// </summary>
public sealed class MedicationLogService : IMedicationLogService
{
    private readonly IAuditLogService _auditService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MedicationLogService> _logger;

    public MedicationLogService(
        ApplicationDbContext dbContext,
        IAuditLogService auditService,
        ILogger<MedicationLogService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MedicationLogOperationResponse> CreateMedicationLogAsync(
        Guid clientId,
        CreateMedicationLogRequest request,
        Guid caregiverId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.MedicationName))
            return new MedicationLogOperationResponse
            {
                Success = false,
                Error = "Medication name is required."
            };

        if (string.IsNullOrWhiteSpace(request.Dosage))
            return new MedicationLogOperationResponse
            {
                Success = false,
                Error = "Dosage is required."
            };

        // Verify client exists
        var client = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

        if (client is null)
            return new MedicationLogOperationResponse
            {
                Success = false,
                Error = "Client not found."
            };

        var medicationLog = new MedicationLog
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CaregiverId = caregiverId,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            MedicationName = request.MedicationName.Trim(),
            Dosage = request.Dosage.Trim(),
            Route = request.Route,
            Status = request.Status,
            ScheduledTime = request.ScheduledTime,
            PrescribedBy = request.PrescribedBy?.Trim(),
            Pharmacy = request.Pharmacy?.Trim(),
            RxNumber = request.RxNumber?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.MedicationLogs.Add(medicationLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var caregiverName = await GetUserNameAsync(caregiverId, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.MedicationLogged,
            caregiverId,
            caregiverName,
            "MedicationLog",
            medicationLog.Id.ToString(),
            "Success",
            ipAddress,
            $"Logged medication '{medicationLog.MedicationName}' for client '{client.FirstName} {client.LastName}'",
            cancellationToken);

        _logger.LogInformation(
            "Medication logged for client {ClientId} by caregiver {CaregiverId}",
            clientId, caregiverId);

        return new MedicationLogOperationResponse
        {
            Success = true,
            MedicationLog = MapToDto(medicationLog, caregiverName)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MedicationLogDto>> GetMedicationLogsAsync(
        Guid clientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MedicationLogs
            .AsNoTracking()
            .Include(m => m.Caregiver)
            .Where(m => m.ClientId == clientId);

        if (fromDate.HasValue) query = query.Where(m => m.Timestamp >= fromDate.Value);

        if (toDate.HasValue) query = query.Where(m => m.Timestamp <= toDate.Value);

        var logs = await query
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync(cancellationToken);

        return logs.Select(l => MapToDto(l, GetCaregiverName(l.Caregiver))).ToList();
    }

    /// <inheritdoc />
    public async Task<MedicationLogDto?> GetMedicationLogByIdAsync(
        Guid clientId,
        Guid medicationId,
        CancellationToken cancellationToken = default)
    {
        var log = await _dbContext.MedicationLogs
            .AsNoTracking()
            .Include(m => m.Caregiver)
            .FirstOrDefaultAsync(m => m.Id == medicationId && m.ClientId == clientId, cancellationToken);

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

    private static MedicationLogDto MapToDto(MedicationLog log, string caregiverName)
    {
        return new MedicationLogDto
        {
            Id = log.Id,
            ClientId = log.ClientId,
            CaregiverId = log.CaregiverId,
            CaregiverName = caregiverName,
            Timestamp = log.Timestamp,
            MedicationName = log.MedicationName,
            Dosage = log.Dosage,
            Route = log.Route,
            Status = log.Status,
            ScheduledTime = log.ScheduledTime,
            PrescribedBy = log.PrescribedBy,
            Pharmacy = log.Pharmacy,
            RxNumber = log.RxNumber,
            Notes = log.Notes,
            CreatedAt = log.CreatedAt
        };
    }
}