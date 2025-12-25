using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Models.CareLog;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
/// Service implementation for client care timeline operations.
/// </summary>
public sealed class TimelineService : ITimelineService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TimelineService> _logger;

    public TimelineService(
        ApplicationDbContext dbContext,
        ILogger<TimelineService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TimelineResponse> GetClientTimelineAsync(
        Guid clientId,
        TimelineQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParams);

        var entries = new List<TimelineEntryDto>();

        // Determine which entry types to include
        var includeAll = queryParams.EntryTypes is null || queryParams.EntryTypes.Count == 0;
        var entryTypesSet = queryParams.EntryTypes is not null
            ? new HashSet<string>(queryParams.EntryTypes, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>();

        // Fetch ADL logs
        if (includeAll || entryTypesSet.Contains(TimelineEntryTypes.ADL))
        {
            var adlEntries = await GetADLEntriesAsync(clientId, queryParams.FromDate, queryParams.ToDate, cancellationToken);
            entries.AddRange(adlEntries);
        }

        // Fetch Vitals logs
        if (includeAll || entryTypesSet.Contains(TimelineEntryTypes.Vitals))
        {
            var vitalsEntries = await GetVitalsEntriesAsync(clientId, queryParams.FromDate, queryParams.ToDate, cancellationToken);
            entries.AddRange(vitalsEntries);
        }

        // Fetch Medication logs
        if (includeAll || entryTypesSet.Contains(TimelineEntryTypes.Medication))
        {
            var medicationEntries = await GetMedicationEntriesAsync(clientId, queryParams.FromDate, queryParams.ToDate, cancellationToken);
            entries.AddRange(medicationEntries);
        }

        // Fetch ROM logs
        if (includeAll || entryTypesSet.Contains(TimelineEntryTypes.ROM))
        {
            var romEntries = await GetROMEntriesAsync(clientId, queryParams.FromDate, queryParams.ToDate, cancellationToken);
            entries.AddRange(romEntries);
        }

        // Fetch Behavior notes
        if (includeAll || entryTypesSet.Contains(TimelineEntryTypes.BehaviorNote))
        {
            var behaviorEntries = await GetBehaviorEntriesAsync(clientId, queryParams.FromDate, queryParams.ToDate, cancellationToken);
            entries.AddRange(behaviorEntries);
        }

        // Fetch Activities
        if (includeAll || entryTypesSet.Contains(TimelineEntryTypes.Activity))
        {
            var activityEntries = await GetActivityEntriesAsync(clientId, queryParams.FromDate, queryParams.ToDate, cancellationToken);
            entries.AddRange(activityEntries);
        }

        // Sort all entries by timestamp (descending)
        var sortedEntries = entries
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        // Apply pagination
        var totalCount = sortedEntries.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize);
        var skip = (queryParams.PageNumber - 1) * queryParams.PageSize;

        var pagedEntries = sortedEntries
            .Skip(skip)
            .Take(queryParams.PageSize)
            .ToList();

        return new TimelineResponse
        {
            Entries = pagedEntries,
            TotalCount = totalCount,
            PageSize = queryParams.PageSize,
            PageNumber = queryParams.PageNumber,
            TotalPages = totalPages
        };
    }

    private async Task<List<TimelineEntryDto>> GetADLEntriesAsync(
        Guid clientId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ADLLogs
            .AsNoTracking()
            .Include(a => a.Caregiver)
            .Where(a => a.ClientId == clientId);

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= toDate.Value);
        }

        var logs = await query.ToListAsync(cancellationToken);

        return logs.Select(l => new TimelineEntryDto
        {
            Id = l.Id,
            EntryType = TimelineEntryTypes.ADL,
            Timestamp = l.Timestamp,
            CaregiverId = l.CaregiverId,
            CaregiverName = GetCaregiverName(l.Caregiver),
            Summary = GetADLSummary(l),
            Details = new
            {
                l.Bathing,
                l.Dressing,
                l.Toileting,
                l.Transferring,
                l.Continence,
                l.Feeding,
                KatzScore = l.CalculateKatzScore(),
                l.Notes
            },
            CreatedAt = l.CreatedAt
        }).ToList();
    }

    private async Task<List<TimelineEntryDto>> GetVitalsEntriesAsync(
        Guid clientId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.VitalsLogs
            .AsNoTracking()
            .Include(v => v.Caregiver)
            .Where(v => v.ClientId == clientId);

        if (fromDate.HasValue)
        {
            query = query.Where(v => v.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(v => v.Timestamp <= toDate.Value);
        }

        var logs = await query.ToListAsync(cancellationToken);

        return logs.Select(l => new TimelineEntryDto
        {
            Id = l.Id,
            EntryType = TimelineEntryTypes.Vitals,
            Timestamp = l.Timestamp,
            CaregiverId = l.CaregiverId,
            CaregiverName = GetCaregiverName(l.Caregiver),
            Summary = GetVitalsSummary(l),
            Details = new
            {
                l.SystolicBP,
                l.DiastolicBP,
                BloodPressure = l.BloodPressure,
                l.Pulse,
                l.Temperature,
                l.TemperatureUnit,
                l.OxygenSaturation,
                l.Notes
            },
            CreatedAt = l.CreatedAt
        }).ToList();
    }

    private async Task<List<TimelineEntryDto>> GetMedicationEntriesAsync(
        Guid clientId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MedicationLogs
            .AsNoTracking()
            .Include(m => m.Caregiver)
            .Where(m => m.ClientId == clientId);

        if (fromDate.HasValue)
        {
            query = query.Where(m => m.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(m => m.Timestamp <= toDate.Value);
        }

        var logs = await query.ToListAsync(cancellationToken);

        return logs.Select(l => new TimelineEntryDto
        {
            Id = l.Id,
            EntryType = TimelineEntryTypes.Medication,
            Timestamp = l.Timestamp,
            CaregiverId = l.CaregiverId,
            CaregiverName = GetCaregiverName(l.Caregiver),
            Summary = GetMedicationSummary(l),
            Details = new
            {
                l.MedicationName,
                l.Dosage,
                l.Route,
                l.Status,
                l.ScheduledTime,
                l.PrescribedBy,
                l.Pharmacy,
                l.RxNumber,
                l.Notes
            },
            CreatedAt = l.CreatedAt
        }).ToList();
    }

    private async Task<List<TimelineEntryDto>> GetROMEntriesAsync(
        Guid clientId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
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

        var logs = await query.ToListAsync(cancellationToken);

        return logs.Select(l => new TimelineEntryDto
        {
            Id = l.Id,
            EntryType = TimelineEntryTypes.ROM,
            Timestamp = l.Timestamp,
            CaregiverId = l.CaregiverId,
            CaregiverName = GetCaregiverName(l.Caregiver),
            Summary = $"{l.ActivityDescription}" +
                (l.Duration.HasValue ? $" ({l.Duration}min)" : "") +
                (l.Repetitions.HasValue ? $" ({l.Repetitions} reps)" : ""),
            Details = new
            {
                l.ActivityDescription,
                l.Duration,
                l.Repetitions,
                l.Notes
            },
            CreatedAt = l.CreatedAt
        }).ToList();
    }

    private async Task<List<TimelineEntryDto>> GetBehaviorEntriesAsync(
        Guid clientId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.BehaviorNotes
            .AsNoTracking()
            .Include(b => b.Caregiver)
            .Where(b => b.ClientId == clientId);

        if (fromDate.HasValue)
        {
            query = query.Where(b => b.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(b => b.Timestamp <= toDate.Value);
        }

        var notes = await query.ToListAsync(cancellationToken);

        return notes.Select(n => new TimelineEntryDto
        {
            Id = n.Id,
            EntryType = TimelineEntryTypes.BehaviorNote,
            Timestamp = n.Timestamp,
            CaregiverId = n.CaregiverId,
            CaregiverName = GetCaregiverName(n.Caregiver),
            Summary = $"[{n.Category}] {TruncateText(n.NoteText, 100)}",
            Details = new
            {
                n.Category,
                n.NoteText,
                n.Severity
            },
            CreatedAt = n.CreatedAt
        }).ToList();
    }

    private async Task<List<TimelineEntryDto>> GetActivityEntriesAsync(
        Guid clientId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        // Query from Activities to avoid cycle in Include path
        // Filter to activities where the client is a participant
        var query = _dbContext.Activities
            .AsNoTracking()
            .Include(a => a.CreatedBy)
            .Include(a => a.Participants)
                .ThenInclude(p => p.Client)
            .Where(a => a.Participants.Any(p => p.ClientId == clientId));

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Date <= toDate.Value);
        }

        var activities = await query.ToListAsync(cancellationToken);

        return activities
            .Select(a => new TimelineEntryDto
            {
                Id = a.Id,
                EntryType = TimelineEntryTypes.Activity,
                Timestamp = a.Date,
                CaregiverId = a.CreatedById,
                CaregiverName = a.CreatedBy is not null
                    ? $"{a.CreatedBy.FirstName} {a.CreatedBy.LastName}"
                    : "Unknown",
                Summary = GetActivitySummary(a),
                Details = new
                {
                    a.ActivityName,
                    a.Description,
                    a.Category,
                    a.IsGroupActivity,
                    a.Duration,
                    Participants = a.Participants
                        .Where(part => part.Client is not null)
                        .Select(part => part.Client!.FirstName + " " + part.Client.LastName)
                        .ToList()
                },
                CreatedAt = a.CreatedAt
            })
            .ToList();
    }

    private static string GetCaregiverName(ApplicationUser? user)
    {
        return user is not null ? $"{user.FirstName} {user.LastName}" : "Unknown";
    }

    private static string GetADLSummary(ADLLog log)
    {
        var parts = new List<string>();

        if (log.Bathing.HasValue) parts.Add($"Bathing: {log.Bathing}");
        if (log.Dressing.HasValue) parts.Add($"Dressing: {log.Dressing}");
        if (log.Toileting.HasValue) parts.Add($"Toileting: {log.Toileting}");
        if (log.Transferring.HasValue) parts.Add($"Transferring: {log.Transferring}");
        if (log.Continence.HasValue) parts.Add($"Continence: {log.Continence}");
        if (log.Feeding.HasValue) parts.Add($"Feeding: {log.Feeding}");

        var summary = string.Join(", ", parts.Take(3));
        if (parts.Count > 3)
        {
            summary += $" (+{parts.Count - 3} more)";
        }

        return $"Katz Score: {log.CalculateKatzScore()} - {summary}";
    }

    private static string GetVitalsSummary(VitalsLog log)
    {
        var parts = new List<string>();

        if (log.SystolicBP.HasValue && log.DiastolicBP.HasValue)
        {
            parts.Add($"BP: {log.BloodPressure}");
        }

        if (log.Pulse.HasValue)
        {
            parts.Add($"Pulse: {log.Pulse}");
        }

        if (log.Temperature.HasValue)
        {
            var unit = log.TemperatureUnit == Domain.Enums.TemperatureUnit.Fahrenheit ? "°F" : "°C";
            parts.Add($"Temp: {log.Temperature}{unit}");
        }

        if (log.OxygenSaturation.HasValue)
        {
            parts.Add($"O2: {log.OxygenSaturation}%");
        }

        return string.Join(", ", parts);
    }

    private static string GetMedicationSummary(MedicationLog log)
    {
        var statusText = log.Status switch
        {
            Domain.Enums.MedicationStatus.Administered => "✓",
            Domain.Enums.MedicationStatus.Refused => "(Refused)",
            Domain.Enums.MedicationStatus.NotAvailable => "(Not Available)",
            Domain.Enums.MedicationStatus.Held => "(Held)",
            Domain.Enums.MedicationStatus.GivenEarly => "(Given Early)",
            Domain.Enums.MedicationStatus.GivenLate => "(Given Late)",
            _ => ""
        };

        return $"{log.MedicationName} {log.Dosage} ({log.Route}) {statusText}".Trim();
    }

    private static string GetActivitySummary(Activity activity)
    {
        var participantCount = activity.Participants.Count;
        var duration = activity.Duration.HasValue ? $" ({activity.Duration}min)" : "";

        return activity.IsGroupActivity
            ? $"[Group] {activity.ActivityName}{duration} - {participantCount} participant(s)"
            : $"{activity.ActivityName}{duration}";
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..(maxLength - 3)] + "...";
    }
}
