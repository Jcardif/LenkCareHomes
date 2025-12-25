using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.Reports;

/// <summary>
///     Service for generating report data with aggregation.
/// </summary>
public sealed class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ClientSummaryReportData?> GetClientSummaryDataAsync(
        Guid clientId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating client summary report for client {ClientId} from {StartDate} to {EndDate}",
            clientId, startDate, endDate);

        // Get client information
        var client = await _context.Clients
            .Include(c => c.Home)
            .Include(c => c.Bed)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

        if (client is null)
        {
            _logger.LogWarning("Client {ClientId} not found", clientId);
            return null;
        }

        var clientInfo = new ClientReportInfo
        {
            Id = client.Id,
            FullName = client.FullName,
            DateOfBirth = client.DateOfBirth,
            Gender = client.Gender,
            AdmissionDate = client.AdmissionDate,
            HomeName = client.Home?.Name ?? "Unknown",
            BedLabel = client.Bed?.Label,
            PrimaryPhysician = client.PrimaryPhysician,
            PrimaryPhysicianPhone = client.PrimaryPhysicianPhone,
            EmergencyContactName = client.EmergencyContactName,
            EmergencyContactPhone = client.EmergencyContactPhone,
            EmergencyContactRelationship = client.EmergencyContactRelationship,
            Allergies = client.Allergies,
            Diagnoses = client.Diagnoses
        };

        // Fetch all logs sequentially (DbContext is not thread-safe for parallel operations)
        var adlLogs = await GetADLLogsAsync(clientId, startDate, endDate, cancellationToken);
        var vitalsLogs = await GetVitalsLogsAsync(clientId, startDate, endDate, cancellationToken);
        var romLogs = await GetROMLogsAsync(clientId, startDate, endDate, cancellationToken);
        var behaviorNotes = await GetBehaviorNotesAsync(clientId, startDate, endDate, cancellationToken);
        var activities = await GetClientActivitiesAsync(clientId, startDate, endDate, cancellationToken);
        var incidents = await GetClientIncidentsAsync(clientId, startDate, endDate, cancellationToken);
        var appointments = await GetClientAppointmentsAsync(clientId, startDate, endDate, cancellationToken);

        // Calculate summary statistics
        var stats = CalculateClientStats(adlLogs, vitalsLogs, romLogs, behaviorNotes, activities, incidents,
            appointments);

        _logger.LogInformation(
            "Client report data aggregated: {ADLCount} ADLs, {VitalsCount} Vitals, {ROMCount} ROM, {BehaviorCount} Behavior, {ActivityCount} Activities, {IncidentCount} Incidents, {AppointmentCount} Appointments",
            adlLogs.Count, vitalsLogs.Count, romLogs.Count, behaviorNotes.Count, activities.Count, incidents.Count,
            appointments.Count);

        return new ClientSummaryReportData
        {
            Client = clientInfo,
            Stats = stats,
            ADLLogs = adlLogs,
            VitalsLogs = vitalsLogs,
            ROMLogs = romLogs,
            BehaviorNotes = behaviorNotes,
            Activities = activities,
            Incidents = incidents,
            Appointments = appointments,
            ReportStartDate = startDate,
            ReportEndDate = endDate,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<HomeSummaryReportData?> GetHomeSummaryDataAsync(
        Guid homeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating home summary report for home {HomeId} from {StartDate} to {EndDate}",
            homeId, startDate, endDate);

        // Get home information
        var home = await _context.Homes
            .Include(h => h.Clients.Where(c => c.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == homeId, cancellationToken);

        if (home is null)
        {
            _logger.LogWarning("Home {HomeId} not found", homeId);
            return null;
        }

        var homeInfo = new HomeReportInfo
        {
            Id = home.Id,
            Name = home.Name,
            Address = home.Address,
            City = home.City,
            State = home.State,
            ZipCode = home.ZipCode,
            PhoneNumber = home.PhoneNumber,
            Capacity = home.Capacity,
            ActiveClientsCount = home.Clients.Count
        };

        // Get all active client IDs
        var clientIds = home.Clients.Select(c => c.Id).ToList();

        // Fetch client summaries
        var clientSummaries = await GetClientSummariesForHomeAsync(clientIds, startDate, endDate, cancellationToken);

        // Fetch home-level incidents and activities
        var incidents = await GetHomeIncidentsAsync(homeId, startDate, endDate, cancellationToken);
        var activities = await GetHomeActivitiesAsync(homeId, startDate, endDate, cancellationToken);
        var appointments = await GetHomeAppointmentsAsync(homeId, startDate, endDate, cancellationToken);

        // Calculate home statistics
        var stats = CalculateHomeStats(clientSummaries, incidents, appointments);

        _logger.LogInformation(
            "Home report data aggregated: {ClientCount} Clients, {IncidentCount} Incidents, {ActivityCount} Activities, {AppointmentCount} Appointments",
            clientSummaries.Count, incidents.Count, activities.Count, appointments.Count);

        return new HomeSummaryReportData
        {
            Home = homeInfo,
            Stats = stats,
            ClientSummaries = clientSummaries,
            Incidents = incidents,
            Activities = activities,
            Appointments = appointments,
            ReportStartDate = startDate,
            ReportEndDate = endDate,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<bool> ClientExistsAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Clients.AnyAsync(c => c.Id == clientId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HomeExistsAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Homes.AnyAsync(h => h.Id == homeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid?> GetClientHomeIdAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .Where(c => c.Id == clientId)
            .Select(c => (Guid?)c.HomeId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ADLLogReportEntry>> GetADLLogsAsync(
        Guid clientId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _context.ADLLogs
            .Include(a => a.Caregiver)
            .Where(a => a.ClientId == clientId && a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderBy(a => a.Timestamp)
            .AsNoTracking()
            .Select(a => new ADLLogReportEntry
            {
                Timestamp = a.Timestamp,
                CaregiverName = a.Caregiver != null ? $"{a.Caregiver.FirstName} {a.Caregiver.LastName}" : "Unknown",
                Bathing = a.Bathing,
                Dressing = a.Dressing,
                Toileting = a.Toileting,
                Transferring = a.Transferring,
                Continence = a.Continence,
                Feeding = a.Feeding,
                KatzScore = a.CalculateKatzScore(),
                Notes = a.Notes
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<VitalsLogReportEntry>> GetVitalsLogsAsync(
        Guid clientId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _context.VitalsLogs
            .Include(v => v.Caregiver)
            .Where(v => v.ClientId == clientId && v.Timestamp >= startDate && v.Timestamp <= endDate)
            .OrderBy(v => v.Timestamp)
            .AsNoTracking()
            .Select(v => new VitalsLogReportEntry
            {
                Timestamp = v.Timestamp,
                CaregiverName = v.Caregiver != null ? $"{v.Caregiver.FirstName} {v.Caregiver.LastName}" : "Unknown",
                BloodPressure = v.BloodPressure,
                Pulse = v.Pulse,
                Temperature = v.Temperature,
                TemperatureUnit = v.TemperatureUnit,
                OxygenSaturation = v.OxygenSaturation,
                Notes = v.Notes
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ROMLogReportEntry>> GetROMLogsAsync(
        Guid clientId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _context.ROMLogs
            .Include(r => r.Caregiver)
            .Where(r => r.ClientId == clientId && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .OrderBy(r => r.Timestamp)
            .AsNoTracking()
            .Select(r => new ROMLogReportEntry
            {
                Timestamp = r.Timestamp,
                CaregiverName = r.Caregiver != null ? $"{r.Caregiver.FirstName} {r.Caregiver.LastName}" : "Unknown",
                ActivityDescription = r.ActivityDescription,
                Duration = r.Duration,
                Repetitions = r.Repetitions,
                Notes = r.Notes
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<BehaviorNoteReportEntry>> GetBehaviorNotesAsync(
        Guid clientId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _context.BehaviorNotes
            .Include(b => b.Caregiver)
            .Where(b => b.ClientId == clientId && b.Timestamp >= startDate && b.Timestamp <= endDate)
            .OrderBy(b => b.Timestamp)
            .AsNoTracking()
            .Select(b => new BehaviorNoteReportEntry
            {
                Timestamp = b.Timestamp,
                CaregiverName = b.Caregiver != null ? $"{b.Caregiver.FirstName} {b.Caregiver.LastName}" : "Unknown",
                Category = b.Category,
                NoteText = b.NoteText,
                Severity = b.Severity
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ActivityReportEntry>> GetClientActivitiesAsync(
        Guid clientId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _context.ActivityParticipants
            .Include(ap => ap.Activity)
            .ThenInclude(a => a!.CreatedBy)
            .Where(ap => ap.ClientId == clientId && ap.Activity!.Date >= startDate && ap.Activity.Date <= endDate)
            .OrderBy(ap => ap.Activity!.Date)
            .AsNoTracking()
            .Select(ap => new ActivityReportEntry
            {
                Date = ap.Activity!.Date,
                ActivityName = ap.Activity.ActivityName,
                Description = ap.Activity.Description,
                Category = ap.Activity.Category,
                Duration = ap.Activity.Duration,
                IsGroupActivity = ap.Activity.IsGroupActivity,
                CreatedByName = ap.Activity.CreatedBy != null
                    ? $"{ap.Activity.CreatedBy.FirstName} {ap.Activity.CreatedBy.LastName}"
                    : "Unknown"
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AppointmentReportEntry>> GetClientAppointmentsAsync(
        Guid clientId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _context.Appointments
            .Where(a => a.ClientId == clientId && a.ScheduledAt >= startDate && a.ScheduledAt <= endDate)
            .OrderBy(a => a.ScheduledAt)
            .AsNoTracking()
            .Select(a => new AppointmentReportEntry
            {
                ScheduledAt = a.ScheduledAt,
                Title = a.Title,
                AppointmentType = a.AppointmentType,
                Status = a.Status,
                DurationMinutes = a.DurationMinutes ?? 0,
                Location = a.Location,
                ProviderName = a.ProviderName,
                Notes = a.Notes,
                OutcomeNotes = a.OutcomeNotes,
                ClientName = null
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<IncidentReportEntry>> GetClientIncidentsAsync(
        Guid clientId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _context.Incidents
            .Include(i => i.ReportedBy)
            .Include(i => i.Client)
            .Where(i => i.ClientId == clientId
                        && i.OccurredAt >= startDate
                        && i.OccurredAt <= endDate
                        && i.Status != IncidentStatus.Draft) // Only include submitted incidents
            .OrderBy(i => i.OccurredAt)
            .AsNoTracking()
            .Select(i => new IncidentReportEntry
            {
                IncidentNumber = i.IncidentNumber,
                OccurredAt = i.OccurredAt,
                IncidentType = i.IncidentType,
                Severity = i.Severity,
                Status = i.Status,
                Location = i.Location,
                Description = i.Description,
                ActionsTaken = i.ActionsTaken,
                ReportedByName = i.ReportedBy != null
                    ? $"{i.ReportedBy.FirstName} {i.ReportedBy.LastName}"
                    : "Unknown",
                ClientName = i.Client != null ? i.Client.FullName : null
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<IncidentReportEntry>> GetHomeIncidentsAsync(
        Guid homeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _context.Incidents
            .Include(i => i.ReportedBy)
            .Include(i => i.Client)
            .Where(i => i.HomeId == homeId
                        && i.OccurredAt >= startDate
                        && i.OccurredAt <= endDate
                        && i.Status != IncidentStatus.Draft) // Only include submitted incidents
            .OrderBy(i => i.OccurredAt)
            .AsNoTracking()
            .Select(i => new IncidentReportEntry
            {
                IncidentNumber = i.IncidentNumber,
                OccurredAt = i.OccurredAt,
                IncidentType = i.IncidentType,
                Severity = i.Severity,
                Status = i.Status,
                Location = i.Location,
                Description = i.Description,
                ActionsTaken = i.ActionsTaken,
                ReportedByName = i.ReportedBy != null
                    ? $"{i.ReportedBy.FirstName} {i.ReportedBy.LastName}"
                    : "Unknown",
                ClientName = i.Client != null ? i.Client.FullName : null
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ActivityReportEntry>> GetHomeActivitiesAsync(
        Guid homeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _context.Activities
            .Include(a => a.CreatedBy)
            .Where(a => a.HomeId == homeId && a.Date >= startDate && a.Date <= endDate)
            .OrderBy(a => a.Date)
            .AsNoTracking()
            .Select(a => new ActivityReportEntry
            {
                Date = a.Date,
                ActivityName = a.ActivityName,
                Description = a.Description,
                Category = a.Category,
                Duration = a.Duration,
                IsGroupActivity = a.IsGroupActivity,
                CreatedByName = a.CreatedBy != null
                    ? $"{a.CreatedBy.FirstName} {a.CreatedBy.LastName}"
                    : "Unknown"
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AppointmentReportEntry>> GetHomeAppointmentsAsync(
        Guid homeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _context.Appointments
            .Include(a => a.Client)
            .Where(a => a.HomeId == homeId && a.ScheduledAt >= startDate && a.ScheduledAt <= endDate)
            .OrderBy(a => a.ScheduledAt)
            .AsNoTracking()
            .Select(a => new AppointmentReportEntry
            {
                ScheduledAt = a.ScheduledAt,
                Title = a.Title,
                AppointmentType = a.AppointmentType,
                Status = a.Status,
                DurationMinutes = a.DurationMinutes ?? 0,
                Location = a.Location,
                ProviderName = a.ProviderName,
                Notes = a.Notes,
                OutcomeNotes = a.OutcomeNotes,
                ClientName = a.Client != null ? a.Client.FullName : null
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ClientSummaryForHomeReport>> GetClientSummariesForHomeAsync(
        List<Guid> clientIds, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        if (clientIds.Count == 0) return [];

        // Get client basic info
        var clients = await _context.Clients
            .Include(c => c.Bed)
            .Where(c => clientIds.Contains(c.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Get counts for each log type
        var adlCounts = await _context.ADLLogs
            .Where(a => clientIds.Contains(a.ClientId) && a.Timestamp >= startDate && a.Timestamp <= endDate)
            .GroupBy(a => a.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId, x => x.Count, cancellationToken);

        var vitalsCounts = await _context.VitalsLogs
            .Where(v => clientIds.Contains(v.ClientId) && v.Timestamp >= startDate && v.Timestamp <= endDate)
            .GroupBy(v => v.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId, x => x.Count, cancellationToken);

        var romCounts = await _context.ROMLogs
            .Where(r => clientIds.Contains(r.ClientId) && r.Timestamp >= startDate && r.Timestamp <= endDate)
            .GroupBy(r => r.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId, x => x.Count, cancellationToken);

        var behaviorCounts = await _context.BehaviorNotes
            .Where(b => clientIds.Contains(b.ClientId) && b.Timestamp >= startDate && b.Timestamp <= endDate)
            .GroupBy(b => b.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId, x => x.Count, cancellationToken);

        var activityCounts = await _context.ActivityParticipants
            .Include(ap => ap.Activity)
            .Where(ap => clientIds.Contains(ap.ClientId) && ap.Activity!.Date >= startDate &&
                         ap.Activity.Date <= endDate)
            .GroupBy(ap => ap.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId, x => x.Count, cancellationToken);

        var incidentCounts = await _context.Incidents
            .Where(i => i.ClientId.HasValue
                        && clientIds.Contains(i.ClientId.Value)
                        && i.OccurredAt >= startDate
                        && i.OccurredAt <= endDate
                        && i.Status != IncidentStatus.Draft)
            .GroupBy(i => i.ClientId!.Value)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId, x => x.Count, cancellationToken);

        var appointmentCounts = await _context.Appointments
            .Where(a => clientIds.Contains(a.ClientId) && a.ScheduledAt >= startDate && a.ScheduledAt <= endDate)
            .GroupBy(a => a.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId, x => x.Count, cancellationToken);

        return clients.Select(c => new ClientSummaryForHomeReport
        {
            Id = c.Id,
            FullName = c.FullName,
            BedLabel = c.Bed?.Label,
            ADLLogsCount = adlCounts.GetValueOrDefault(c.Id, 0),
            VitalsLogsCount = vitalsCounts.GetValueOrDefault(c.Id, 0),
            ROMLogsCount = romCounts.GetValueOrDefault(c.Id, 0),
            BehaviorNotesCount = behaviorCounts.GetValueOrDefault(c.Id, 0),
            ActivitiesCount = activityCounts.GetValueOrDefault(c.Id, 0),
            IncidentsCount = incidentCounts.GetValueOrDefault(c.Id, 0),
            AppointmentsCount = appointmentCounts.GetValueOrDefault(c.Id, 0)
        }).ToList();
    }

    private static ClientReportSummaryStats CalculateClientStats(
        IReadOnlyList<ADLLogReportEntry> adlLogs,
        IReadOnlyList<VitalsLogReportEntry> vitalsLogs,
        IReadOnlyList<ROMLogReportEntry> romLogs,
        IReadOnlyList<BehaviorNoteReportEntry> behaviorNotes,
        IReadOnlyList<ActivityReportEntry> activities,
        IReadOnlyList<IncidentReportEntry> incidents,
        IReadOnlyList<AppointmentReportEntry> appointments)
    {
        double? avgKatz = adlLogs.Count > 0 ? adlLogs.Average(a => a.KatzScore) : null;

        // Parse blood pressure for averaging
        var systolicValues = new List<int>();
        var diastolicValues = new List<int>();
        foreach (var vital in vitalsLogs)
            if (!string.IsNullOrEmpty(vital.BloodPressure) && vital.BloodPressure.Contains('/'))
            {
                var parts = vital.BloodPressure.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[0], out var sys) && int.TryParse(parts[1], out var dia))
                {
                    systolicValues.Add(sys);
                    diastolicValues.Add(dia);
                }
            }

        var pulseValues = vitalsLogs.Where(v => v.Pulse.HasValue).Select(v => v.Pulse!.Value).ToList();
        var oxygenValues = vitalsLogs.Where(v => v.OxygenSaturation.HasValue).Select(v => v.OxygenSaturation!.Value)
            .ToList();

        return new ClientReportSummaryStats
        {
            TotalADLLogs = adlLogs.Count,
            TotalVitalsLogs = vitalsLogs.Count,
            TotalROMLogs = romLogs.Count,
            TotalBehaviorNotes = behaviorNotes.Count,
            TotalActivities = activities.Count,
            TotalIncidents = incidents.Count,
            TotalAppointments = appointments.Count,
            AverageKatzScore = avgKatz,
            AverageSystolicBP = systolicValues.Count > 0 ? systolicValues.Average() : null,
            AverageDiastolicBP = diastolicValues.Count > 0 ? diastolicValues.Average() : null,
            AveragePulse = pulseValues.Count > 0 ? pulseValues.Average() : null,
            AverageOxygenSaturation = oxygenValues.Count > 0 ? oxygenValues.Average() : null
        };
    }

    private static HomeReportSummaryStats CalculateHomeStats(
        IReadOnlyList<ClientSummaryForHomeReport> clientSummaries,
        IReadOnlyList<IncidentReportEntry> incidents,
        IReadOnlyList<AppointmentReportEntry> appointments)
    {
        var incidentsByType = incidents
            .GroupBy(i => i.IncidentType)
            .ToDictionary(g => g.Key, g => g.Count());

        return new HomeReportSummaryStats
        {
            TotalClients = clientSummaries.Count,
            TotalADLLogs = clientSummaries.Sum(c => c.ADLLogsCount),
            TotalVitalsLogs = clientSummaries.Sum(c => c.VitalsLogsCount),
            TotalROMLogs = clientSummaries.Sum(c => c.ROMLogsCount),
            TotalBehaviorNotes = clientSummaries.Sum(c => c.BehaviorNotesCount),
            TotalActivities = clientSummaries.Sum(c => c.ActivitiesCount),
            TotalIncidents = incidents.Count,
            TotalAppointments = appointments.Count,
            IncidentsByType = incidentsByType
        };
    }
}