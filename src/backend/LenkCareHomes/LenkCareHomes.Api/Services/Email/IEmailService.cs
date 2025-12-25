namespace LenkCareHomes.Api.Services.Email;

/// <summary>
/// Service interface for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a user invitation email.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="firstName">User's first name.</param>
    /// <param name="invitationLink">The invitation link with token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendInvitationEmailAsync(
        string toEmail,
        string firstName,
        string invitationLink,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset email.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="firstName">User's first name.</param>
    /// <param name="resetLink">The password reset link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordResetEmailAsync(
        string toEmail,
        string firstName,
        string resetLink,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an MFA reset notification email.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="firstName">User's first name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendMfaResetEmailAsync(
        string toEmail,
        string firstName,
        CancellationToken cancellationToken = default);
}
