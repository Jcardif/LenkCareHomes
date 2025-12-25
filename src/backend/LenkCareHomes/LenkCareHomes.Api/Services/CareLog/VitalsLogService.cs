using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.CareLog;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
///     Service implementation for vitals log operations.
/// </summary>
public sealed class VitalsLogService : IVitalsLogService
{
    // Validation ranges for vitals
    private const int MinSystolicBP = 50;
    private const int MaxSystolicBP = 300;
    private const int MinDiastolicBP = 30;
    private const int MaxDiastolicBP = 200;
    private const int MinPulse = 30;
    private const int MaxPulse = 200;
    private const decimal MinTempF = 90m;
    private const decimal MaxTempF = 110m;
    private const decimal MinTempC = 32m;
    private const decimal MaxTempC = 43m;
    private const int MinO2Sat = 70;
    private const int MaxO2Sat = 100;
    private readonly IAuditLogService _auditService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<VitalsLogService> _logger;

    public VitalsLogService(
        ApplicationDbContext dbContext,
        IAuditLogService auditService,
        ILogger<VitalsLogService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<VitalsLogOperationResponse> CreateVitalsLogAsync(
        Guid clientId,
        CreateVitalsLogRequest request,
        Guid caregiverId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate at least one vital is recorded
        if (!request.SystolicBP.HasValue &&
            !request.DiastolicBP.HasValue &&
            !request.Pulse.HasValue &&
            !request.Temperature.HasValue &&
            !request.OxygenSaturation.HasValue)
            return new VitalsLogOperationResponse
            {
                Success = false,
                Error = "At least one vital sign must be recorded."
            };

        // Validate ranges
        var validationError = ValidateVitals(request);
        if (validationError is not null)
            return new VitalsLogOperationResponse
            {
                Success = false,
                Error = validationError
            };

        // Verify client exists
        var client = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

        if (client is null)
            return new VitalsLogOperationResponse
            {
                Success = false,
                Error = "Client not found."
            };

        var vitalsLog = new VitalsLog
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CaregiverId = caregiverId,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            SystolicBP = request.SystolicBP,
            DiastolicBP = request.DiastolicBP,
            Pulse = request.Pulse,
            Temperature = request.Temperature,
            TemperatureUnit = request.TemperatureUnit ?? TemperatureUnit.Fahrenheit,
            OxygenSaturation = request.OxygenSaturation,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.VitalsLogs.Add(vitalsLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var caregiverName = await GetUserNameAsync(caregiverId, cancellationToken);

        await _auditService.LogPhiAccessAsync(
            AuditActions.VitalsLogged,
            caregiverId,
            caregiverName,
            "VitalsLog",
            vitalsLog.Id.ToString(),
            "Success",
            ipAddress,
            $"Logged vitals for client '{client.FirstName} {client.LastName}'",
            cancellationToken);

        _logger.LogInformation(
            "Vitals logged for client {ClientId} by caregiver {CaregiverId}",
            clientId, caregiverId);

        return new VitalsLogOperationResponse
        {
            Success = true,
            VitalsLog = MapToDto(vitalsLog, caregiverName)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VitalsLogDto>> GetVitalsLogsAsync(
        Guid clientId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VitalsLogs
            .AsNoTracking()
            .Include(v => v.Caregiver)
            .Where(v => v.ClientId == clientId);

        if (fromDate.HasValue) query = query.Where(v => v.Timestamp >= fromDate.Value);

        if (toDate.HasValue) query = query.Where(v => v.Timestamp <= toDate.Value);

        var logs = await query
            .OrderByDescending(v => v.Timestamp)
            .ToListAsync(cancellationToken);

        return logs.Select(l => MapToDto(l, GetCaregiverName(l.Caregiver))).ToList();
    }

    /// <inheritdoc />
    public async Task<VitalsLogDto?> GetVitalsLogByIdAsync(
        Guid clientId,
        Guid vitalsId,
        CancellationToken cancellationToken = default)
    {
        var log = await _dbContext.VitalsLogs
            .AsNoTracking()
            .Include(v => v.Caregiver)
            .FirstOrDefaultAsync(v => v.Id == vitalsId && v.ClientId == clientId, cancellationToken);

        return log is null ? null : MapToDto(log, GetCaregiverName(log.Caregiver));
    }

    private static string? ValidateVitals(CreateVitalsLogRequest request)
    {
        if (request.SystolicBP.HasValue && (request.SystolicBP < MinSystolicBP || request.SystolicBP > MaxSystolicBP))
            return $"Systolic blood pressure must be between {MinSystolicBP} and {MaxSystolicBP} mmHg.";

        if (request.DiastolicBP.HasValue &&
            (request.DiastolicBP < MinDiastolicBP || request.DiastolicBP > MaxDiastolicBP))
            return $"Diastolic blood pressure must be between {MinDiastolicBP} and {MaxDiastolicBP} mmHg.";

        if (request.Pulse.HasValue && (request.Pulse < MinPulse || request.Pulse > MaxPulse))
            return $"Pulse must be between {MinPulse} and {MaxPulse} bpm.";

        if (request.Temperature.HasValue)
        {
            var unit = request.TemperatureUnit ?? TemperatureUnit.Fahrenheit;
            var minTemp = unit == TemperatureUnit.Fahrenheit ? MinTempF : MinTempC;
            var maxTemp = unit == TemperatureUnit.Fahrenheit ? MaxTempF : MaxTempC;

            if (request.Temperature < minTemp || request.Temperature > maxTemp)
            {
                var unitLabel = unit == TemperatureUnit.Fahrenheit ? "°F" : "°C";
                return $"Temperature must be between {minTemp} and {maxTemp} {unitLabel}.";
            }
        }

        if (request.OxygenSaturation.HasValue &&
            (request.OxygenSaturation < MinO2Sat || request.OxygenSaturation > MaxO2Sat))
            return $"Oxygen saturation must be between {MinO2Sat}% and {MaxO2Sat}%.";

        return null;
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

    private static VitalsLogDto MapToDto(VitalsLog log, string caregiverName)
    {
        return new VitalsLogDto
        {
            Id = log.Id,
            ClientId = log.ClientId,
            CaregiverId = log.CaregiverId,
            CaregiverName = caregiverName,
            Timestamp = log.Timestamp,
            SystolicBP = log.SystolicBP,
            DiastolicBP = log.DiastolicBP,
            BloodPressure = log.BloodPressure,
            Pulse = log.Pulse,
            Temperature = log.Temperature,
            TemperatureUnit = log.TemperatureUnit,
            OxygenSaturation = log.OxygenSaturation,
            Notes = log.Notes,
            CreatedAt = log.CreatedAt
        };
    }
}