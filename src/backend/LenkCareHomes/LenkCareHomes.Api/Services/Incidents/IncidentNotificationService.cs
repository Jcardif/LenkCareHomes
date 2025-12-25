using Azure;
using Azure.Communication.Email;
using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Domain.Enums;
using LenkCareHomes.Api.Services.Audit;
using LenkCareHomes.Api.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LenkCareHomes.Api.Services.Incidents;

/// <summary>
///     Service for sending incident notifications to admins.
/// </summary>
public sealed class IncidentNotificationService : IIncidentNotificationService
{
    private readonly IAuditLogService _auditLogService;
    private readonly ApplicationDbContext _dbContext;
    private readonly EmailClient? _emailClient;
    private readonly ILogger<IncidentNotificationService> _logger;
    private readonly EmailSettings _settings;

    public IncidentNotificationService(
        ApplicationDbContext dbContext,
        IOptions<EmailSettings> emailSettings,
        IAuditLogService auditLogService,
        ILogger<IncidentNotificationService> logger)
    {
        _dbContext = dbContext;
        _settings = emailSettings.Value;
        _auditLogService = auditLogService;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_settings.ConnectionString))
            _emailClient = new EmailClient(_settings.ConnectionString);
        else
            _logger.LogWarning("Email service not configured. Incident notifications will be logged instead of sent.");
    }

    /// <inheritdoc />
    public async Task NotifyAdminsOfNewIncidentAsync(
        Guid incidentId,
        string clientName,
        string homeName,
        IncidentType incidentType,
        string reportedByName,
        CancellationToken cancellationToken = default)
    {
        // Get all active admins
        var adminRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == Roles.Admin, cancellationToken);

        if (adminRole is null)
        {
            _logger.LogWarning("Admin role not found. Cannot send incident notifications.");
            return;
        }

        var adminEmails = await _dbContext.UserRoles
            .Where(ur => ur.RoleId == adminRole.Id)
            .Join(_dbContext.Users,
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => u)
            .Where(u => u.IsActive && u.Email != null)
            .Select(u => new { u.Email, u.FirstName })
            .ToListAsync(cancellationToken);

        if (adminEmails.Count == 0)
        {
            _logger.LogWarning("No active admins found. Cannot send incident notifications.");
            return;
        }

        var subject = $"[URGENT] New Incident Report - {incidentType} at {homeName}";
        var htmlContent = GenerateHtmlContent(incidentId, clientName, homeName, incidentType, reportedByName);
        var plainTextContent = GeneratePlainTextContent(incidentId, clientName, homeName, incidentType, reportedByName);

        foreach (var admin in adminEmails)
        {
            if (string.IsNullOrWhiteSpace(admin.Email)) continue;

            try
            {
                await SendEmailAsync(admin.Email, subject, htmlContent, plainTextContent, cancellationToken);

                _logger.LogInformation(
                    "Incident notification sent to admin {Email} for incident {IncidentId}",
                    admin.Email,
                    incidentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send incident notification to admin {Email}", admin.Email);
            }
        }

        // Log the notification event
        await _auditLogService.LogAsync(new AuditLogEntry
        {
            PartitionKey = "system",
            Action = AuditActions.IncidentNotificationSent,
            ResourceType = "Incident",
            ResourceId = incidentId.ToString(),
            Outcome = AuditOutcome.Success,
            Details = $"Notification sent to {adminEmails.Count} admin(s) for new incident"
        }, cancellationToken);
    }

    private static string GenerateHtmlContent(
        Guid incidentId,
        string clientName,
        string homeName,
        IncidentType incidentType,
        string reportedByName)
    {
        return $"""
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="utf-8">
                </head>
                <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
                    <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
                        <div style="background-color: #ff4d4f; color: white; padding: 15px; border-radius: 4px 4px 0 0;">
                            <h2 style="margin: 0;">⚠️ New Incident Report</h2>
                        </div>
                        <div style="border: 1px solid #ddd; border-top: none; padding: 20px; border-radius: 0 0 4px 4px;">
                            <p>A new incident has been reported and requires your attention.</p>
                            
                            <table style="width: 100%; border-collapse: collapse; margin: 20px 0;">
                                <tr>
                                    <td style="padding: 8px; border-bottom: 1px solid #eee; font-weight: bold; width: 140px;">Incident Type:</td>
                                    <td style="padding: 8px; border-bottom: 1px solid #eee; color: #ff4d4f; font-weight: bold;">{incidentType}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px; border-bottom: 1px solid #eee; font-weight: bold;">Client:</td>
                                    <td style="padding: 8px; border-bottom: 1px solid #eee;">{clientName}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px; border-bottom: 1px solid #eee; font-weight: bold;">Home:</td>
                                    <td style="padding: 8px; border-bottom: 1px solid #eee;">{homeName}</td>
                                </tr>
                                <tr>
                                    <td style="padding: 8px; border-bottom: 1px solid #eee; font-weight: bold;">Reported By:</td>
                                    <td style="padding: 8px; border-bottom: 1px solid #eee;">{reportedByName}</td>
                                </tr>
                            </table>

                            <p>Please log in to LenkCare Homes to review the full incident report and take appropriate action.</p>
                        </div>
                        <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">
                        <p style="font-size: 12px; color: #666;">
                            This is an automated urgent notification from LenkCare Homes. Please do not reply to this email.<br>
                            Incident ID: {incidentId}
                        </p>
                    </div>
                </body>
                </html>
                """;
    }

    private static string GeneratePlainTextContent(
        Guid incidentId,
        string clientName,
        string homeName,
        IncidentType incidentType,
        string reportedByName)
    {
        return $"""
                ⚠️ NEW INCIDENT REPORT

                A new incident has been reported and requires your attention.

                Incident Type: {incidentType}
                Client: {clientName}
                Home: {homeName}
                Reported By: {reportedByName}

                Please log in to LenkCare Homes to review the full incident report and take appropriate action.

                ---
                Incident ID: {incidentId}
                This is an automated urgent notification from LenkCare Homes.
                """;
    }

    private async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlContent,
        string plainTextContent,
        CancellationToken cancellationToken)
    {
        if (_emailClient is null)
        {
            _logger.LogInformation(
                "Incident notification would be sent - To: {ToEmail}, Subject: {Subject}",
                toEmail,
                subject);
            return;
        }

        var emailMessage = new EmailMessage(
            _settings.SenderAddress,
            content: new EmailContent(subject)
            {
                Html = htmlContent,
                PlainText = plainTextContent
            },
            recipients: new EmailRecipients([new EmailAddress(toEmail)]));

        var operation = await _emailClient.SendAsync(WaitUntil.Started, emailMessage, cancellationToken);
        _logger.LogInformation(
            "Incident notification email sent to {ToEmail}, Operation ID: {OperationId}",
            toEmail,
            operation.Id);
    }
}