using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Models.Reports;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LenkCareHomes.Api.Services.Reports;

/// <summary>
///     Service for generating PDF reports using QuestPDF.
/// </summary>
public sealed class PdfReportService : IPdfReportService
{
    private const string ConfidentialityText = "CONFIDENTIAL - Contains Protected Health Information (PHI)";

    // Colors
    private static readonly string PrimaryColor = "#2d3732";
    private static readonly string AccentColor = "#5a7a6b";
    private static readonly string HeaderBgColor = "#f0f4f2";
    private static readonly string TableHeaderColor = "#e8eeeb";
    private static readonly string BorderColor = "#d0d7d4";

    public PdfReportService()
    {
        // Configure QuestPDF license for community use
        Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public byte[] GenerateClientSummaryPdf(ClientSummaryReportData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(PrimaryColor));

                page.Header().Element(c => ComposeHeader(c, $"Client Summary Report: {data.Client.FullName}"));

                page.Content().Element(c => ComposeClientContent(c, data));

                page.Footer().Element(c => ComposeFooter(c, data.GeneratedAt));
            });
        });

        return document.GeneratePdf();
    }

    /// <inheritdoc />
    public byte[] GenerateHomeSummaryPdf(HomeSummaryReportData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(PrimaryColor));

                page.Header().Element(c => ComposeHeader(c, $"Home Summary Report: {data.Home.Name}"));

                page.Content().Element(c => ComposeHomeContent(c, data));

                page.Footer().Element(c => ComposeFooter(c, data.GeneratedAt));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, string title)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("LenkCare Homes")
                        .FontSize(20)
                        .Bold()
                        .FontColor(AccentColor);
                    col.Item().Text(title)
                        .FontSize(14)
                        .SemiBold();
                });
            });

            column.Item().PaddingTop(5).BorderBottom(1).BorderColor(BorderColor);
        });
    }

    private static void ComposeFooter(IContainer container, DateTime generatedAt)
    {
        container.Column(column =>
        {
            column.Item().BorderTop(1).BorderColor(BorderColor).PaddingTop(5);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text(ConfidentialityText)
                    .FontSize(8)
                    .FontColor(Colors.Red.Medium);

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span($"Generated: {generatedAt:yyyy-MM-dd HH:mm UTC} | Page ")
                        .FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" of ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        });
    }

    private static void ComposeClientContent(IContainer container, ClientSummaryReportData data)
    {
        container.Column(column =>
        {
            column.Spacing(15);

            // Report Period
            column.Item().Background(HeaderBgColor).Padding(10).Row(row =>
            {
                row.RelativeItem()
                    .Text($"Report Period: {data.ReportStartDate:MMM dd, yyyy} - {data.ReportEndDate:MMM dd, yyyy}")
                    .SemiBold();
            });

            // Client Demographics Section
            column.Item().Element(c => ComposeClientDemographics(c, data.Client));

            // Summary Statistics Section
            column.Item().Element(c => ComposeClientStatistics(c, data.Stats));

            // ADL Logs Section
            if (data.ADLLogs.Count > 0) column.Item().Element(c => ComposeADLLogsSection(c, data.ADLLogs));

            // Vitals Section
            if (data.VitalsLogs.Count > 0) column.Item().Element(c => ComposeVitalsSection(c, data.VitalsLogs));

            // ROM Section
            if (data.ROMLogs.Count > 0) column.Item().Element(c => ComposeROMSection(c, data.ROMLogs));

            // Behavior Notes Section
            if (data.BehaviorNotes.Count > 0)
                column.Item().Element(c => ComposeBehaviorNotesSection(c, data.BehaviorNotes));

            // Activities Section
            if (data.Activities.Count > 0) column.Item().Element(c => ComposeActivitiesSection(c, data.Activities));

            // Appointments Section
            if (data.Appointments.Count > 0)
                column.Item().Element(c => ComposeAppointmentsSection(c, data.Appointments));

            // Incidents Section
            if (data.Incidents.Count > 0) column.Item().Element(c => ComposeIncidentsSection(c, data.Incidents));
        });
    }

    private static void ComposeClientDemographics(IContainer container, ClientReportInfo client)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSectionHeader(c, "Client Information"));

            column.Item().Border(1).BorderColor(BorderColor).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Name: {client.FullName}").SemiBold();
                    col.Item().Text(
                        $"Date of Birth: {client.DateOfBirth:MMM dd, yyyy} ({CalculateAge(client.DateOfBirth)} years)");
                    col.Item().Text($"Gender: {client.Gender}");
                    col.Item().Text($"Admission Date: {client.AdmissionDate:MMM dd, yyyy}");
                    col.Item().Text($"Home: {client.HomeName}");
                    if (!string.IsNullOrEmpty(client.BedLabel)) col.Item().Text($"Bed: {client.BedLabel}");
                });

                row.RelativeItem().Column(col =>
                {
                    if (!string.IsNullOrEmpty(client.PrimaryPhysician))
                    {
                        col.Item().Text($"Physician: {client.PrimaryPhysician}");
                        if (!string.IsNullOrEmpty(client.PrimaryPhysicianPhone))
                            col.Item().Text($"Phone: {client.PrimaryPhysicianPhone}");
                    }

                    if (!string.IsNullOrEmpty(client.EmergencyContactName))
                    {
                        col.Item().PaddingTop(5).Text("Emergency Contact:").SemiBold();
                        col.Item().Text($"{client.EmergencyContactName}");
                        if (!string.IsNullOrEmpty(client.EmergencyContactPhone))
                            col.Item().Text($"{client.EmergencyContactPhone}");
                        if (!string.IsNullOrEmpty(client.EmergencyContactRelationship))
                            col.Item().Text($"({client.EmergencyContactRelationship})");
                    }
                });
            });

            // Allergies and Diagnoses
            if (!string.IsNullOrEmpty(client.Allergies) || !string.IsNullOrEmpty(client.Diagnoses))
                column.Item().PaddingTop(5).Border(1).BorderColor(BorderColor).Padding(10).Column(col =>
                {
                    if (!string.IsNullOrEmpty(client.Allergies))
                    {
                        col.Item().Text("Allergies:").Bold().FontColor(Colors.Red.Medium);
                        col.Item().Text(client.Allergies);
                    }

                    if (!string.IsNullOrEmpty(client.Diagnoses))
                    {
                        col.Item().PaddingTop(5).Text("Diagnoses:").Bold();
                        col.Item().Text(client.Diagnoses);
                    }
                });
        });
    }

    private static void ComposeClientStatistics(IContainer container, ClientReportSummaryStats stats)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSectionHeader(c, "Summary Statistics"));

            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(BorderColor).Padding(10).Column(col =>
                {
                    col.Item().Text("Record Counts").SemiBold();
                    col.Item().Text($"ADL Logs: {stats.TotalADLLogs}");
                    col.Item().Text($"Vitals Logs: {stats.TotalVitalsLogs}");
                    col.Item().Text($"ROM Logs: {stats.TotalROMLogs}");
                    col.Item().Text($"Behavior Notes: {stats.TotalBehaviorNotes}");
                    col.Item().Text($"Activities: {stats.TotalActivities}");
                    col.Item().Text($"Appointments: {stats.TotalAppointments}");
                    col.Item().Text($"Incidents: {stats.TotalIncidents}");
                });

                row.RelativeItem().PaddingLeft(10).Border(1).BorderColor(BorderColor).Padding(10).Column(col =>
                {
                    col.Item().Text("Averages").SemiBold();
                    if (stats.AverageKatzScore.HasValue)
                        col.Item().Text($"Avg Katz Score: {stats.AverageKatzScore:F1}");
                    if (stats.AverageSystolicBP.HasValue && stats.AverageDiastolicBP.HasValue)
                        col.Item().Text(
                            $"Avg Blood Pressure: {stats.AverageSystolicBP:F0}/{stats.AverageDiastolicBP:F0}");
                    if (stats.AveragePulse.HasValue) col.Item().Text($"Avg Pulse: {stats.AveragePulse:F0} bpm");
                    if (stats.AverageOxygenSaturation.HasValue)
                        col.Item().Text($"Avg O₂ Saturation: {stats.AverageOxygenSaturation:F1}%");
                });
            });
        });
    }

    private static void ComposeADLLogsSection(IContainer container, IReadOnlyList<ADLLogReportEntry> logs)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSectionHeader(c, $"ADL Logs ({logs.Count})"));

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80); // Date
                    columns.RelativeColumn(); // Caregiver
                    columns.ConstantColumn(55); // Bathing
                    columns.ConstantColumn(55); // Dressing
                    columns.ConstantColumn(55); // Toileting
                    columns.ConstantColumn(55); // Transfer
                    columns.ConstantColumn(55); // Continence
                    columns.ConstantColumn(55); // Feeding
                    columns.ConstantColumn(40); // Score
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Date").Bold();
                    header.Cell().Element(CellStyle).Text("Caregiver").Bold();
                    header.Cell().Element(CellStyle).Text("Bath").Bold();
                    header.Cell().Element(CellStyle).Text("Dress").Bold();
                    header.Cell().Element(CellStyle).Text("Toilet").Bold();
                    header.Cell().Element(CellStyle).Text("Transfer").Bold();
                    header.Cell().Element(CellStyle).Text("Contin.").Bold();
                    header.Cell().Element(CellStyle).Text("Feed").Bold();
                    header.Cell().Element(CellStyle).Text("Score").Bold();

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.Background(TableHeaderColor).Padding(5);
                    }
                });

                foreach (var log in logs)
                {
                    table.Cell().Element(CellStyle).Text(log.Timestamp.ToString("MM/dd/yy"));
                    table.Cell().Element(CellStyle).Text(TruncateName(log.CaregiverName));
                    table.Cell().Element(CellStyle).Text(FormatADLLevel(log.Bathing));
                    table.Cell().Element(CellStyle).Text(FormatADLLevel(log.Dressing));
                    table.Cell().Element(CellStyle).Text(FormatADLLevel(log.Toileting));
                    table.Cell().Element(CellStyle).Text(FormatADLLevel(log.Transferring));
                    table.Cell().Element(CellStyle).Text(FormatADLLevel(log.Continence));
                    table.Cell().Element(CellStyle).Text(FormatADLLevel(log.Feeding));
                    table.Cell().Element(CellStyle).Text(log.KatzScore.ToString());

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.BorderBottom(1).BorderColor(BorderColor).Padding(5);
                    }
                }
            });
        });
    }

    private static void ComposeVitalsSection(IContainer container, IReadOnlyList<VitalsLogReportEntry> logs)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSectionHeader(c, $"Vitals Logs ({logs.Count})"));

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80); // Date
                    columns.RelativeColumn(); // Caregiver
                    columns.ConstantColumn(60); // BP
                    columns.ConstantColumn(50); // Pulse
                    columns.ConstantColumn(60); // Temp
                    columns.ConstantColumn(50); // O2
                    columns.RelativeColumn(1.5f); // Notes
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Date").Bold();
                    header.Cell().Element(CellStyle).Text("Caregiver").Bold();
                    header.Cell().Element(CellStyle).Text("BP").Bold();
                    header.Cell().Element(CellStyle).Text("Pulse").Bold();
                    header.Cell().Element(CellStyle).Text("Temp").Bold();
                    header.Cell().Element(CellStyle).Text("O₂%").Bold();
                    header.Cell().Element(CellStyle).Text("Notes").Bold();

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.Background(TableHeaderColor).Padding(5);
                    }
                });

                foreach (var log in logs)
                {
                    var tempDisplay = log.Temperature.HasValue
                        ? $"{log.Temperature:F1}°{(log.TemperatureUnit == TemperatureUnit.Fahrenheit ? "F" : "C")}"
                        : "-";

                    table.Cell().Element(CellStyle).Text(log.Timestamp.ToString("MM/dd/yy"));
                    table.Cell().Element(CellStyle).Text(TruncateName(log.CaregiverName));
                    table.Cell().Element(CellStyle).Text(log.BloodPressure ?? "-");
                    table.Cell().Element(CellStyle).Text(log.Pulse?.ToString() ?? "-");
                    table.Cell().Element(CellStyle).Text(tempDisplay);
                    table.Cell().Element(CellStyle).Text(log.OxygenSaturation?.ToString() ?? "-");
                    table.Cell().Element(CellStyle).Text(TruncateText(log.Notes));

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.BorderBottom(1).BorderColor(BorderColor).Padding(5);
                    }
                }
            });
        });
    }

    private static void ComposeROMSection(IContainer container, IReadOnlyList<ROMLogReportEntry> logs)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSectionHeader(c, $"ROM Activities ({logs.Count})"));

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80); // Date
                    columns.RelativeColumn(); // Caregiver
                    columns.RelativeColumn(1.5f); // Activity
                    columns.ConstantColumn(50); // Duration
                    columns.ConstantColumn(40); // Reps
                    columns.RelativeColumn(1.5f); // Notes
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Date").Bold();
                    header.Cell().Element(CellStyle).Text("Caregiver").Bold();
                    header.Cell().Element(CellStyle).Text("Activity").Bold();
                    header.Cell().Element(CellStyle).Text("Min").Bold();
                    header.Cell().Element(CellStyle).Text("Reps").Bold();
                    header.Cell().Element(CellStyle).Text("Notes").Bold();

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.Background(TableHeaderColor).Padding(5);
                    }
                });

                foreach (var log in logs)
                {
                    table.Cell().Element(CellStyle).Text(log.Timestamp.ToString("MM/dd/yy"));
                    table.Cell().Element(CellStyle).Text(TruncateName(log.CaregiverName));
                    table.Cell().Element(CellStyle).Text(TruncateText(log.ActivityDescription, 25));
                    table.Cell().Element(CellStyle).Text(log.Duration?.ToString() ?? "-");
                    table.Cell().Element(CellStyle).Text(log.Repetitions?.ToString() ?? "-");
                    table.Cell().Element(CellStyle).Text(TruncateText(log.Notes, 25));

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.BorderBottom(1).BorderColor(BorderColor).Padding(5);
                    }
                }
            });
        });
    }

    private static void ComposeBehaviorNotesSection(IContainer container, IReadOnlyList<BehaviorNoteReportEntry> notes)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSectionHeader(c, $"Behavior Notes ({notes.Count})"));

            foreach (var note in notes)
                column.Item().Border(1).BorderColor(BorderColor).Padding(8).Column(noteCol =>
                {
                    noteCol.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"{note.Timestamp:MMM dd, yyyy HH:mm}").SemiBold();
                        row.ConstantItem(100).Text($"Category: {note.Category}");
                        if (note.Severity.HasValue) row.ConstantItem(80).Text($"Severity: {note.Severity}");
                    });
                    noteCol.Item().Text($"Caregiver: {note.CaregiverName}").FontSize(9);
                    noteCol.Item().PaddingTop(5).Text(note.NoteText);
                });
        });
    }

    private static void ComposeActivitiesSection(IContainer container, IReadOnlyList<ActivityReportEntry> activities)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSectionHeader(c, $"Activities ({activities.Count})"));

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80); // Date
                    columns.RelativeColumn(1.5f); // Activity
                    columns.RelativeColumn(); // Category
                    columns.ConstantColumn(50); // Duration
                    columns.ConstantColumn(50); // Type
                    columns.RelativeColumn(); // Created By
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Date").Bold();
                    header.Cell().Element(CellStyle).Text("Activity").Bold();
                    header.Cell().Element(CellStyle).Text("Category").Bold();
                    header.Cell().Element(CellStyle).Text("Min").Bold();
                    header.Cell().Element(CellStyle).Text("Type").Bold();
                    header.Cell().Element(CellStyle).Text("Created By").Bold();

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.Background(TableHeaderColor).Padding(5);
                    }
                });

                foreach (var activity in activities)
                {
                    table.Cell().Element(CellStyle).Text(activity.Date.ToString("MM/dd/yy"));
                    table.Cell().Element(CellStyle).Text(TruncateText(activity.ActivityName, 25));
                    table.Cell().Element(CellStyle).Text(activity.Category.ToString());
                    table.Cell().Element(CellStyle).Text(activity.Duration?.ToString() ?? "-");
                    table.Cell().Element(CellStyle).Text(activity.IsGroupActivity ? "Group" : "Indiv");
                    table.Cell().Element(CellStyle).Text(TruncateName(activity.CreatedByName));

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.BorderBottom(1).BorderColor(BorderColor).Padding(5);
                    }
                }
            });
        });
    }

    private static void ComposeAppointmentsSection(IContainer container,
        IReadOnlyList<AppointmentReportEntry> appointments, bool includeClientName = false)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSectionHeader(c, $"Appointments ({appointments.Count})"));

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(100); // Date/Time
                    if (includeClientName) columns.RelativeColumn(); // Client Name
                    columns.RelativeColumn(1.5f); // Title
                    columns.RelativeColumn(0.8f); // Type
                    columns.ConstantColumn(70); // Status
                    columns.ConstantColumn(50); // Duration
                    columns.RelativeColumn(); // Provider
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Date/Time").Bold();
                    if (includeClientName) header.Cell().Element(CellStyle).Text("Client").Bold();
                    header.Cell().Element(CellStyle).Text("Title").Bold();
                    header.Cell().Element(CellStyle).Text("Type").Bold();
                    header.Cell().Element(CellStyle).Text("Status").Bold();
                    header.Cell().Element(CellStyle).Text("Min").Bold();
                    header.Cell().Element(CellStyle).Text("Provider").Bold();

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.Background(TableHeaderColor).Padding(5);
                    }
                });

                foreach (var appt in appointments)
                {
                    table.Cell().Element(CellStyle).Text(appt.ScheduledAt.ToString("MM/dd/yy HH:mm"));
                    if (includeClientName) table.Cell().Element(CellStyle).Text(TruncateName(appt.ClientName ?? ""));
                    table.Cell().Element(CellStyle).Text(TruncateText(appt.Title, 25));
                    table.Cell().Element(CellStyle).Text(appt.AppointmentType.ToString());
                    table.Cell().Element(CellStyle).Text(appt.Status.ToString());
                    table.Cell().Element(CellStyle).Text(appt.DurationMinutes.ToString());
                    table.Cell().Element(CellStyle).Text(TruncateName(appt.ProviderName ?? "-"));

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.BorderBottom(1).BorderColor(BorderColor).Padding(5);
                    }
                }
            });
        });
    }

    private static void ComposeIncidentsSection(IContainer container, IReadOnlyList<IncidentReportEntry> incidents)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSectionHeader(c, $"Incidents ({incidents.Count})"));

            foreach (var incident in incidents)
                column.Item().Border(1).BorderColor(BorderColor).Padding(8).Column(incCol =>
                {
                    incCol.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"{incident.IncidentNumber}").Bold();
                        row.ConstantItem(100).Text($"Type: {incident.IncidentType}");
                        row.ConstantItem(80).Text($"Severity: {incident.Severity}");
                        row.ConstantItem(80).Text($"Status: {incident.Status}");
                    });
                    incCol.Item()
                        .Text($"Occurred: {incident.OccurredAt:MMM dd, yyyy HH:mm} | Location: {incident.Location}")
                        .FontSize(9);
                    incCol.Item().Text($"Reported by: {incident.ReportedByName}").FontSize(9);
                    if (!string.IsNullOrEmpty(incident.ClientName))
                        incCol.Item().Text($"Client: {incident.ClientName}").FontSize(9);
                    incCol.Item().PaddingTop(5).Text("Description:").SemiBold();
                    incCol.Item().Text(incident.Description);
                    if (!string.IsNullOrEmpty(incident.ActionsTaken))
                    {
                        incCol.Item().PaddingTop(5).Text("Actions Taken:").SemiBold();
                        incCol.Item().Text(incident.ActionsTaken);
                    }
                });
        });
    }

    private static void ComposeHomeContent(IContainer container, HomeSummaryReportData data)
    {
        container.Column(column =>
        {
            column.Spacing(15);

            // Report Period
            column.Item().Background(HeaderBgColor).Padding(10).Row(row =>
            {
                row.RelativeItem()
                    .Text($"Report Period: {data.ReportStartDate:MMM dd, yyyy} - {data.ReportEndDate:MMM dd, yyyy}")
                    .SemiBold();
            });

            // Home Information Section
            column.Item().Element(c => ComposeHomeInformation(c, data.Home));

            // Summary Statistics Section
            column.Item().Element(c => ComposeHomeStatistics(c, data.Stats));

            // Client Summary Section
            if (data.ClientSummaries.Count > 0)
                column.Item().Element(c => ComposeClientSummaryTable(c, data.ClientSummaries));

            // Activities Section
            if (data.Activities.Count > 0) column.Item().Element(c => ComposeActivitiesSection(c, data.Activities));

            // Appointments Section
            if (data.Appointments.Count > 0)
                column.Item().Element(c => ComposeAppointmentsSection(c, data.Appointments, true));

            // Incidents Section
            if (data.Incidents.Count > 0) column.Item().Element(c => ComposeIncidentsSection(c, data.Incidents));
        });
    }

    private static void ComposeHomeInformation(IContainer container, HomeReportInfo home)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSectionHeader(c, "Home Information"));

            column.Item().Border(1).BorderColor(BorderColor).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Name: {home.Name}").SemiBold();
                    col.Item().Text($"Address: {home.Address}");
                    col.Item().Text($"{home.City}, {home.State} {home.ZipCode}");
                });

                row.RelativeItem().Column(col =>
                {
                    if (!string.IsNullOrEmpty(home.PhoneNumber)) col.Item().Text($"Phone: {home.PhoneNumber}");
                    col.Item().Text($"Capacity: {home.Capacity} beds");
                    col.Item().Text($"Active Clients: {home.ActiveClientsCount}");
                });
            });
        });
    }

    private static void ComposeHomeStatistics(IContainer container, HomeReportSummaryStats stats)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSectionHeader(c, "Summary Statistics"));

            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(BorderColor).Padding(10).Column(col =>
                {
                    col.Item().Text("Record Counts").SemiBold();
                    col.Item().Text($"Total Clients: {stats.TotalClients}");
                    col.Item().Text($"ADL Logs: {stats.TotalADLLogs}");
                    col.Item().Text($"Vitals Logs: {stats.TotalVitalsLogs}");
                    col.Item().Text($"ROM Logs: {stats.TotalROMLogs}");
                    col.Item().Text($"Behavior Notes: {stats.TotalBehaviorNotes}");
                    col.Item().Text($"Activities: {stats.TotalActivities}");
                    col.Item().Text($"Appointments: {stats.TotalAppointments}");
                    col.Item().Text($"Incidents: {stats.TotalIncidents}");
                });

                if (stats.IncidentsByType.Count > 0)
                    row.RelativeItem().PaddingLeft(10).Border(1).BorderColor(BorderColor).Padding(10).Column(col =>
                    {
                        col.Item().Text("Incidents by Type").SemiBold();
                        foreach (var (type, count) in stats.IncidentsByType.OrderByDescending(x => x.Value))
                            col.Item().Text($"{type}: {count}");
                    });
            });
        });
    }

    private static void ComposeClientSummaryTable(IContainer container,
        IReadOnlyList<ClientSummaryForHomeReport> clients)
    {
        container.Column(column =>
        {
            column.Item().Element(c => ComposeSectionHeader(c, $"Client Activity Summary ({clients.Count} clients)"));

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2); // Name
                    columns.ConstantColumn(50); // Bed
                    columns.ConstantColumn(40); // ADL
                    columns.ConstantColumn(40); // Vitals
                    columns.ConstantColumn(40); // ROM
                    columns.ConstantColumn(45); // Behavior
                    columns.ConstantColumn(45); // Activities
                    columns.ConstantColumn(45); // Appointments
                    columns.ConstantColumn(45); // Incidents
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Client Name").Bold();
                    header.Cell().Element(CellStyle).Text("Bed").Bold();
                    header.Cell().Element(CellStyle).Text("ADL").Bold();
                    header.Cell().Element(CellStyle).Text("Vitals").Bold();
                    header.Cell().Element(CellStyle).Text("ROM").Bold();
                    header.Cell().Element(CellStyle).Text("Behav.").Bold();
                    header.Cell().Element(CellStyle).Text("Activ.").Bold();
                    header.Cell().Element(CellStyle).Text("Appts.").Bold();
                    header.Cell().Element(CellStyle).Text("Incid.").Bold();

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.Background(TableHeaderColor).Padding(5);
                    }
                });

                foreach (var client in clients)
                {
                    table.Cell().Element(CellStyle).Text(TruncateText(client.FullName, 25));
                    table.Cell().Element(CellStyle).Text(client.BedLabel ?? "-");
                    table.Cell().Element(CellStyle).Text(client.ADLLogsCount.ToString());
                    table.Cell().Element(CellStyle).Text(client.VitalsLogsCount.ToString());
                    table.Cell().Element(CellStyle).Text(client.ROMLogsCount.ToString());
                    table.Cell().Element(CellStyle).Text(client.BehaviorNotesCount.ToString());
                    table.Cell().Element(CellStyle).Text(client.ActivitiesCount.ToString());
                    table.Cell().Element(CellStyle).Text(client.AppointmentsCount.ToString());
                    table.Cell().Element(CellStyle).Text(client.IncidentsCount.ToString());

                    static IContainer CellStyle(IContainer c)
                    {
                        return c.BorderBottom(1).BorderColor(BorderColor).Padding(5);
                    }
                }
            });
        });
    }

    private static void ComposeSectionHeader(IContainer container, string title)
    {
        container.Background(AccentColor).Padding(8).Text(title)
            .FontSize(12)
            .Bold()
            .FontColor(Colors.White);
    }

    private static string FormatADLLevel(ADLLevel? level)
    {
        return level switch
        {
            ADLLevel.Independent => "I",
            ADLLevel.PartialAssist => "P",
            ADLLevel.Dependent => "D",
            ADLLevel.NotApplicable => "N/A",
            _ => "-"
        };
    }

    private static string TruncateName(string name, int maxLength = 15)
    {
        if (string.IsNullOrEmpty(name)) return "";
        return name.Length > maxLength ? name[..(maxLength - 2)] + ".." : name;
    }

    private static string TruncateText(string? text, int maxLength = 30)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length > maxLength ? text[..(maxLength - 3)] + "..." : text;
    }

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}