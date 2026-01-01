using LenkCareHomes.Api.Models.Passkey;

namespace LenkCareHomes.Api.Services.Passkey;

/// <summary>
///     Service interface for WebAuthn/FIDO2 passkey operations.
/// </summary>
public interface IPasskeyService
{
    /// <summary>
    ///     Begins the passkey registration process for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="deviceName">The user-friendly name for this device.</param>
    /// <param name="passkeySetupToken">Temporary token for setup flow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration options to pass to the authenticator.</returns>
    Task<PasskeyRegistrationBeginResponse> BeginRegistrationAsync(
        Guid userId,
        string deviceName,
        string? passkeySetupToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Completes the passkey registration process.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The registration completion request.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="userAgent">Client user agent for audit logging.</param>
    /// <param name="passkeySetupToken">Temporary token for setup flow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration result.</returns>
    Task<PasskeyRegistrationCompleteResponse> CompleteRegistrationAsync(
        Guid userId,
        PasskeyRegistrationCompleteRequest request,
        string? ipAddress,
        string? userAgent,
        string? passkeySetupToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Begins the passkey authentication process.
    /// </summary>
    /// <param name="email">Optional email to identify the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication options to pass to the authenticator.</returns>
    Task<PasskeyAuthenticationBeginResponse> BeginAuthenticationAsync(
        string? email,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Completes the passkey authentication process.
    /// </summary>
    /// <param name="request">The authentication completion request.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="userAgent">Client user agent for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result with token if successful.</returns>
    Task<PasskeyAuthenticationCompleteResponse> CompleteAuthenticationAsync(
        PasskeyAuthenticationCompleteRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all passkeys for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of passkeys.</returns>
    Task<PasskeyListResponse> GetUserPasskeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates a passkey's device name.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="passkeyId">The passkey ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if update was successful.</returns>
    Task<bool> UpdatePasskeyAsync(
        Guid userId,
        Guid passkeyId,
        UpdatePasskeyRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a passkey.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="passkeyId">The passkey ID.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deletion result.</returns>
    Task<DeletePasskeyResponse> DeletePasskeyAsync(
        Guid userId,
        Guid passkeyId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the count of active passkeys for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of active passkeys.</returns>
    Task<int> GetPasskeyCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}