using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;

namespace LenkCareHomes.Api.Services.Email;

/// <summary>
///     Azure Communication Services implementation of the email service.
/// </summary>
public sealed class AzureEmailService : IEmailService
{
    private readonly EmailClient? _emailClient;
    private readonly ILogger<AzureEmailService> _logger;
    private readonly EmailSettings _settings;

    public AzureEmailService(
        IOptions<EmailSettings> settings,
        ILogger<AzureEmailService> logger)
    {
        ArgumentNullException.ThrowIfNull(settings?.Value);
        _settings = settings.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_settings.ConnectionString))
            _emailClient = new EmailClient(_settings.ConnectionString);
        else
            _logger.LogWarning("Email service not configured. Emails will be logged instead of sent.");
    }

    /// <inheritdoc />
    public async Task SendInvitationEmailAsync(
        string toEmail,
        string firstName,
        string invitationLink,
        CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to LenkCare Homes - Complete Your Account Setup";
        var htmlContent = $"""
                           <!DOCTYPE html>
                           <html>
                           <head>
                               <meta charset="utf-8">
                           </head>
                           <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
                               <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
                                   <h2 style="color: #1890ff;">Welcome to LenkCare Homes</h2>
                                   <p>Hello {firstName},</p>
                                   <p>You have been invited to join LenkCare Homes. Please click the button below to complete your account setup and configure multi-factor authentication.</p>
                                   <p style="margin: 30px 0;">
                                       <a href="{invitationLink}" 
                                          style="background-color: #1890ff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;">
                                           Complete Account Setup
                                       </a>
                                   </p>
                                   <p>This invitation link will expire in 48 hours.</p>
                                   <p>If you did not expect this invitation, please ignore this email.</p>
                                   <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">
                                   <p style="font-size: 12px; color: #666;">
                                       This is an automated message from LenkCare Homes. Please do not reply to this email.
                                   </p>
                               </div>
                           </body>
                           </html>
                           """;

        var plainTextContent = $"""
                                Welcome to LenkCare Homes

                                Hello {firstName},

                                You have been invited to join LenkCare Homes. Please visit the following link to complete your account setup and configure multi-factor authentication:

                                {invitationLink}

                                This invitation link will expire in 48 hours.

                                If you did not expect this invitation, please ignore this email.
                                """;

        await SendEmailAsync(toEmail, subject, htmlContent, plainTextContent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string firstName,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        var subject = "LenkCare Homes - Password Reset Request";
        var htmlContent = $"""
                           <!DOCTYPE html>
                           <html>
                           <head>
                               <meta charset="utf-8">
                           </head>
                           <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
                               <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
                                   <h2 style="color: #1890ff;">Password Reset Request</h2>
                                   <p>Hello {firstName},</p>
                                   <p>We received a request to reset your password for your LenkCare Homes account. Click the button below to set a new password.</p>
                                   <p style="margin: 30px 0;">
                                       <a href="{resetLink}" 
                                          style="background-color: #1890ff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;">
                                           Reset Password
                                       </a>
                                   </p>
                                   <p>This link will expire in 1 hour.</p>
                                   <p>If you did not request a password reset, please ignore this email. Your password will remain unchanged.</p>
                                   <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">
                                   <p style="font-size: 12px; color: #666;">
                                       This is an automated message from LenkCare Homes. Please do not reply to this email.
                                   </p>
                               </div>
                           </body>
                           </html>
                           """;

        var plainTextContent = $"""
                                Password Reset Request

                                Hello {firstName},

                                We received a request to reset your password for your LenkCare Homes account. Please visit the following link to set a new password:

                                {resetLink}

                                This link will expire in 1 hour.

                                If you did not request a password reset, please ignore this email. Your password will remain unchanged.
                                """;

        await SendEmailAsync(toEmail, subject, htmlContent, plainTextContent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendMfaResetEmailAsync(
        string toEmail,
        string firstName,
        CancellationToken cancellationToken = default)
    {
        var subject = "LenkCare Homes - MFA Has Been Reset";
        var htmlContent = $"""
                           <!DOCTYPE html>
                           <html>
                           <head>
                               <meta charset="utf-8">
                           </head>
                           <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
                               <div style="max-width: 600px; margin: 0 auto; padding: 20px;">
                                   <h2 style="color: #1890ff;">Multi-Factor Authentication Reset</h2>
                                   <p>Hello {firstName},</p>
                                   <p>Your multi-factor authentication (MFA) has been reset by an administrator. You will need to set up MFA again when you next log in to LenkCare Homes.</p>
                                   <p>If you did not request this change, please contact your administrator immediately.</p>
                                   <hr style="border: none; border-top: 1px solid #eee; margin: 30px 0;">
                                   <p style="font-size: 12px; color: #666;">
                                       This is an automated message from LenkCare Homes. Please do not reply to this email.
                                   </p>
                               </div>
                           </body>
                           </html>
                           """;

        var plainTextContent = $"""
                                Multi-Factor Authentication Reset

                                Hello {firstName},

                                Your multi-factor authentication (MFA) has been reset by an administrator. You will need to set up MFA again when you next log in to LenkCare Homes.

                                If you did not request this change, please contact your administrator immediately.
                                """;

        await SendEmailAsync(toEmail, subject, htmlContent, plainTextContent, cancellationToken);
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
            _logger.LogInformation("Email would be sent - To: {ToEmail}, Subject: {Subject}", toEmail, subject);
            return;
        }

        try
        {
            var emailMessage = new EmailMessage(
                _settings.SenderAddress,
                content: new EmailContent(subject)
                {
                    Html = htmlContent,
                    PlainText = plainTextContent
                },
                recipients: new EmailRecipients([new EmailAddress(toEmail)]));

            var operation = await _emailClient.SendAsync(WaitUntil.Started, emailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {ToEmail}, Operation ID: {OperationId}", toEmail,
                operation.Id);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            throw;
        }
    }
}

/// <summary>
///     Configuration settings for email service.
/// </summary>
public sealed class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    ///     Gets or sets the Azure Communication Services connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the sender email address.
    /// </summary>
    public string SenderAddress { get; set; } = "noreply@lenkcarehomes.com";
}