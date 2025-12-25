using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Models.Reports;

/// <summary>
/// Request to generate a client summary report.
/// </summary>
public sealed record GenerateClientReportRequest
{
    /// <summary>
    /// Gets or sets the client ID.
    /// </summary>
    public Guid ClientId { get; init; }

    /// <summary>
    /// Gets or sets the start date for the report period.
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// Gets or sets the end date for the report period.
    /// </summary>
    public DateTime EndDate { get; init; }
}

/// <summary>
/// Request to generate a home summary report.
/// </summary>
public sealed record GenerateHomeReportRequest
{
    /// <summary>
    /// Gets or sets the home ID.
    /// </summary>
    public Guid HomeId { get; init; }

    /// <summary>
    /// Gets or sets the start date for the report period.
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// Gets or sets the end date for the report period.
    /// </summary>
    public DateTime EndDate { get; init; }
}

/// <summary>
/// Response containing the generated report information.
/// </summary>
public sealed record ReportGenerationResponse
{
    /// <summary>
    /// Gets or sets whether the report generation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the error message if generation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets or sets the generated report ID.
    /// </summary>
    public Guid? ReportId { get; init; }

    /// <summary>
    /// Gets or sets the report file name.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets or sets the PDF content as bytes.
    /// </summary>
    public byte[]? PdfContent { get; init; }

    /// <summary>
    /// Creates a successful response with PDF content.
    /// </summary>
    public static ReportGenerationResponse WithContent(Guid reportId, string fileName, byte[] pdfContent) => new()
    {
        Success = true,
        ReportId = reportId,
        FileName = fileName,
        ContentType = "application/pdf",
        PdfContent = pdfContent
    };

    /// <summary>
    /// Creates a failure response.
    /// </summary>
    public static ReportGenerationResponse Failure(string error) => new()
    {
        Success = false,
        Error = error
    };
}

/// <summary>
/// DTO for client demographic information in reports.
/// </summary>
public sealed record ClientReportInfo
{
    public Guid Id { get; init; }
    public required string FullName { get; init; }
    public DateTime DateOfBirth { get; init; }
    public required string Gender { get; init; }
    public DateTime AdmissionDate { get; init; }
    public required string HomeName { get; init; }
    public string? BedLabel { get; init; }
    public string? PrimaryPhysician { get; init; }
    public string? PrimaryPhysicianPhone { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? EmergencyContactRelationship { get; init; }
    public string? Allergies { get; init; }
    public string? Diagnoses { get; init; }
}

/// <summary>
/// DTO for home information in reports.
/// </summary>
public sealed record HomeReportInfo
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string ZipCode { get; init; }
    public string? PhoneNumber { get; init; }
    public int Capacity { get; init; }
    public int ActiveClientsCount { get; init; }
}

/// <summary>
/// DTO for ADL log entry in reports.
/// </summary>
public sealed record ADLLogReportEntry
{
    public DateTime Timestamp { get; init; }
    public required string CaregiverName { get; init; }
    public ADLLevel? Bathing { get; init; }
    public ADLLevel? Dressing { get; init; }
    public ADLLevel? Toileting { get; init; }
    public ADLLevel? Transferring { get; init; }
    public ADLLevel? Continence { get; init; }
    public ADLLevel? Feeding { get; init; }
    public int KatzScore { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// DTO for vitals log entry in reports.
/// </summary>
public sealed record VitalsLogReportEntry
{
    public DateTime Timestamp { get; init; }
    public required string CaregiverName { get; init; }
    public string? BloodPressure { get; init; }
    public int? Pulse { get; init; }
    public decimal? Temperature { get; init; }
    public TemperatureUnit TemperatureUnit { get; init; }
    public int? OxygenSaturation { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// DTO for ROM log entry in reports.
/// </summary>
public sealed record ROMLogReportEntry
{
    public DateTime Timestamp { get; init; }
    public required string CaregiverName { get; init; }
    public required string ActivityDescription { get; init; }
    public int? Duration { get; init; }
    public int? Repetitions { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// DTO for behavior note entry in reports.
/// </summary>
public sealed record BehaviorNoteReportEntry
{
    public DateTime Timestamp { get; init; }
    public required string CaregiverName { get; init; }
    public BehaviorCategory Category { get; init; }
    public required string NoteText { get; init; }
    public NoteSeverity? Severity { get; init; }
}

/// <summary>
/// DTO for activity entry in reports.
/// </summary>
public sealed record ActivityReportEntry
{
    public DateTime Date { get; init; }
    public required string ActivityName { get; init; }
    public string? Description { get; init; }
    public ActivityCategory Category { get; init; }
    public int? Duration { get; init; }
    public bool IsGroupActivity { get; init; }
    public required string CreatedByName { get; init; }
}

/// <summary>
/// DTO for incident entry in reports.
/// </summary>
public sealed record IncidentReportEntry
{
    public required string IncidentNumber { get; init; }
    public DateTime OccurredAt { get; init; }
    public IncidentType IncidentType { get; init; }
    public int Severity { get; init; }
    public IncidentStatus Status { get; init; }
    public required string Location { get; init; }
    public required string Description { get; init; }
    public string? ActionsTaken { get; init; }
    public required string ReportedByName { get; init; }
    public string? ClientName { get; init; }
}

/// <summary>
/// DTO for appointment entry in reports.
/// </summary>
public sealed record AppointmentReportEntry
{
    public DateTime ScheduledAt { get; init; }
    public required string Title { get; init; }
    public AppointmentType AppointmentType { get; init; }
    public AppointmentStatus Status { get; init; }
    public int DurationMinutes { get; init; }
    public string? Location { get; init; }
    public string? ProviderName { get; init; }
    public string? Notes { get; init; }
    public string? OutcomeNotes { get; init; }
    public string? ClientName { get; init; }
}

/// <summary>
/// Summary statistics for a client report.
/// </summary>
public sealed record ClientReportSummaryStats
{
    public int TotalADLLogs { get; init; }
    public int TotalVitalsLogs { get; init; }
    public int TotalROMLogs { get; init; }
    public int TotalBehaviorNotes { get; init; }
    public int TotalActivities { get; init; }
    public int TotalIncidents { get; init; }
    public int TotalAppointments { get; init; }
    public double? AverageKatzScore { get; init; }
    public double? AverageSystolicBP { get; init; }
    public double? AverageDiastolicBP { get; init; }
    public double? AveragePulse { get; init; }
    public double? AverageOxygenSaturation { get; init; }
}

/// <summary>
/// Summary statistics for a home report.
/// </summary>
public sealed record HomeReportSummaryStats
{
    public int TotalClients { get; init; }
    public int TotalADLLogs { get; init; }
    public int TotalVitalsLogs { get; init; }
    public int TotalROMLogs { get; init; }
    public int TotalBehaviorNotes { get; init; }
    public int TotalActivities { get; init; }
    public int TotalIncidents { get; init; }
    public int TotalAppointments { get; init; }
    public Dictionary<IncidentType, int> IncidentsByType { get; init; } = [];
}

/// <summary>
/// Client summary for home reports.
/// </summary>
public sealed record ClientSummaryForHomeReport
{
    public Guid Id { get; init; }
    public required string FullName { get; init; }
    public string? BedLabel { get; init; }
    public int ADLLogsCount { get; init; }
    public int VitalsLogsCount { get; init; }
    public int ROMLogsCount { get; init; }
    public int BehaviorNotesCount { get; init; }
    public int ActivitiesCount { get; init; }
    public int IncidentsCount { get; init; }
    public int AppointmentsCount { get; init; }
}

/// <summary>
/// Aggregated data for a client summary report.
/// </summary>
public sealed record ClientSummaryReportData
{
    public required ClientReportInfo Client { get; init; }
    public required ClientReportSummaryStats Stats { get; init; }
    public IReadOnlyList<ADLLogReportEntry> ADLLogs { get; init; } = [];
    public IReadOnlyList<VitalsLogReportEntry> VitalsLogs { get; init; } = [];
    public IReadOnlyList<ROMLogReportEntry> ROMLogs { get; init; } = [];
    public IReadOnlyList<BehaviorNoteReportEntry> BehaviorNotes { get; init; } = [];
    public IReadOnlyList<ActivityReportEntry> Activities { get; init; } = [];
    public IReadOnlyList<IncidentReportEntry> Incidents { get; init; } = [];
    public IReadOnlyList<AppointmentReportEntry> Appointments { get; init; } = [];
    public DateTime ReportStartDate { get; init; }
    public DateTime ReportEndDate { get; init; }
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Aggregated data for a home summary report.
/// </summary>
public sealed record HomeSummaryReportData
{
    public required HomeReportInfo Home { get; init; }
    public required HomeReportSummaryStats Stats { get; init; }
    public IReadOnlyList<ClientSummaryForHomeReport> ClientSummaries { get; init; } = [];
    public IReadOnlyList<IncidentReportEntry> Incidents { get; init; } = [];
    public IReadOnlyList<ActivityReportEntry> Activities { get; init; } = [];
    public IReadOnlyList<AppointmentReportEntry> Appointments { get; init; } = [];
    public DateTime ReportStartDate { get; init; }
    public DateTime ReportEndDate { get; init; }
    public DateTime GeneratedAt { get; init; }
}
