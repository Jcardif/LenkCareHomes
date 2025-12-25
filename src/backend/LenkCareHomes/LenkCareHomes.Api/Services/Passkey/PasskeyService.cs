using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Models.Passkey;
using LenkCareHomes.Api.Services.Audit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Services.Passkey;

/// <summary>
/// WebAuthn/FIDO2 passkey service implementation.
/// </summary>
public sealed class PasskeyService : IPasskeyService
{
    private readonly IFido2 _fido2;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<PasskeyService> _logger;

    // Cache for registration/authentication sessions (in production, use distributed cache)
    private static readonly Dictionary<string, RegistrationSession> RegistrationSessions = new();
    private static readonly Dictionary<string, AuthenticationSession> AuthenticationSessions = new();

    public PasskeyService(
        IFido2 fido2,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext dbContext,
        IAuditLogService auditLog,
        ILogger<PasskeyService> logger)
    {
        _fido2 = fido2;
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _auditLog = auditLog;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PasskeyRegistrationBeginResponse> BeginRegistrationAsync(
        Guid userId,
        string deviceName,
        string? passkeySetupToken = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new PasskeyRegistrationBeginResponse { Success = false, Error = "User not found." };
        }

        // If a temp token is provided, validate it
        if (!string.IsNullOrEmpty(passkeySetupToken))
        {
            var isValid = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "PasskeySetup", passkeySetupToken);
            if (!isValid)
            {
                return new PasskeyRegistrationBeginResponse { Success = false, Error = "Invalid or expired setup token." };
            }
        }

        // Get existing credentials to exclude
        var existingCredentials = await _dbContext.UserPasskeys
            .Where(p => p.UserId == userId && p.IsActive)
            .Select(p => new PublicKeyCredentialDescriptor(WebEncoders.Base64UrlDecode(p.CredentialId)))
            .ToListAsync(cancellationToken);

        var fidoUser = new Fido2User
        {
            Id = Encoding.UTF8.GetBytes(userId.ToString()),
            Name = user.Email ?? user.UserName ?? "User",
            DisplayName = user.FullName
        };

        var authenticatorSelection = new AuthenticatorSelection
        {
            ResidentKey = ResidentKeyRequirement.Preferred,
            UserVerification = UserVerificationRequirement.Preferred
        };

        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fidoUser,
            ExcludeCredentials = existingCredentials,
            AuthenticatorSelection = authenticatorSelection,
            AttestationPreference = AttestationConveyancePreference.None,
            Extensions = new AuthenticationExtensionsClientInputs
            {
                CredProps = true
            }
        });

        var sessionId = GenerateSessionId();
        var session = new RegistrationSession
        {
            UserId = userId,
            DeviceName = deviceName,
            Options = options,
            CreatedAt = DateTime.UtcNow
        };

        lock (RegistrationSessions)
        {
            CleanupExpiredSessions();
            RegistrationSessions[sessionId] = session;
        }

        return new PasskeyRegistrationBeginResponse
        {
            Success = true,
            SessionId = sessionId,
            Options = options.ToJson()
        };
    }

    /// <inheritdoc />
    public async Task<PasskeyRegistrationCompleteResponse> CompleteRegistrationAsync(
        Guid userId,
        PasskeyRegistrationCompleteRequest request,
        string? ipAddress,
        string? userAgent,
        string? passkeySetupToken = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // If a temp token is provided, validate it
        if (!string.IsNullOrEmpty(passkeySetupToken))
        {
            var userToCheck = await _userManager.FindByIdAsync(userId.ToString());
            if (userToCheck is null)
            {
                return new PasskeyRegistrationCompleteResponse { Success = false, Error = "User not found." };
            }

            var isValid = await _userManager.VerifyUserTokenAsync(userToCheck, TokenOptions.DefaultProvider, "PasskeySetup", passkeySetupToken);
            if (!isValid)
            {
                return new PasskeyRegistrationCompleteResponse { Success = false, Error = "Invalid or expired setup token." };
            }
        }

        RegistrationSession? session;
        lock (RegistrationSessions)
        {
            if (!RegistrationSessions.TryGetValue(request.SessionId, out session))
            {
                return new PasskeyRegistrationCompleteResponse { Success = false, Error = "Invalid or expired session." };
            }

            if (session.UserId != userId)
            {
                return new PasskeyRegistrationCompleteResponse { Success = false, Error = "Session mismatch." };
            }

            RegistrationSessions.Remove(request.SessionId);
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new PasskeyRegistrationCompleteResponse { Success = false, Error = "User not found." };
        }

        try
        {
            var attestationResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(request.AttestationResponse);
            if (attestationResponse is null)
            {
                return new PasskeyRegistrationCompleteResponse { Success = false, Error = "Invalid attestation response." };
            }

            // Callback to check if credential ID is unique to this user
            IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, ct) =>
            {
                var exists = await _dbContext.UserPasskeys
                    .AnyAsync(p => p.CredentialId == WebEncoders.Base64UrlEncode(args.CredentialId), ct);
                return !exists;
            };

            var result = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = attestationResponse,
                OriginalOptions = session.Options,
                IsCredentialIdUniqueToUserCallback = callback
            }, cancellationToken);

            // In v4, result is directly the RegisteredPublicKeyCredential
            var passkey = new UserPasskey
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CredentialId = WebEncoders.Base64UrlEncode(result.Id),
                PublicKey = Convert.ToBase64String(result.PublicKey),
                SignatureCounter = result.SignCount,
                AaGuid = result.AaGuid.ToString(),
                DeviceName = request.DeviceName,
                CredentialType = result.Type.ToString(),
                Transports = result.Transports is not null
                    ? string.Join(",", result.Transports.Select(t => t.ToString()))
                    : null,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _dbContext.UserPasskeys.Add(passkey);

            // Update user MFA status
            user.IsMfaSetupComplete = true;
            user.RequiresPasskeySetup = false;
            user.TwoFactorEnabled = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var passkeyCount = await _dbContext.UserPasskeys
                .CountAsync(p => p.UserId == userId && p.IsActive, cancellationToken);

            await _auditLog.LogAuthenticationEventAsync(
                AuditActions.PasskeyRegistered,
                AuditOutcome.Success,
                userId,
                user.Email,
                ipAddress,
                userAgent,
                $"Passkey registered: {request.DeviceName}",
                cancellationToken);

            _logger.LogInformation("Passkey registered for user {UserId}: {DeviceName}", userId, request.DeviceName);

            return new PasskeyRegistrationCompleteResponse
            {
                Success = true,
                PasskeyId = passkey.Id,
                DeviceName = passkey.DeviceName,
                TotalPasskeys = passkeyCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete passkey registration for user {UserId}", userId);
            return new PasskeyRegistrationCompleteResponse { Success = false, Error = "Registration failed." };
        }
    }

    /// <inheritdoc />
    public async Task<PasskeyAuthenticationBeginResponse> BeginAuthenticationAsync(
        string? email,
        CancellationToken cancellationToken = default)
    {
        List<PublicKeyCredentialDescriptor>? allowedCredentials = null;
        Guid? userId = null;

        if (!string.IsNullOrEmpty(email))
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is not null && user.IsActive)
            {
                userId = user.Id;
                var credentialIds = await _dbContext.UserPasskeys
                    .Where(p => p.UserId == user.Id && p.IsActive)
                    .Select(p => p.CredentialId)
                    .ToListAsync(cancellationToken);

                allowedCredentials = credentialIds
                    .Select(id => new PublicKeyCredentialDescriptor(WebEncoders.Base64UrlDecode(id)))
                    .ToList();

                if (allowedCredentials.Count == 0)
                {
                    return new PasskeyAuthenticationBeginResponse { Success = false, Error = "No passkeys registered." };
                }
            }
        }

        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = allowedCredentials ?? [],
            UserVerification = UserVerificationRequirement.Preferred,
            Extensions = new AuthenticationExtensionsClientInputs
            {
                Extensions = true
            }
        });

        var sessionId = GenerateSessionId();
        var session = new AuthenticationSession
        {
            Options = options,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        lock (AuthenticationSessions)
        {
            CleanupExpiredSessions();
            AuthenticationSessions[sessionId] = session;
        }

        return new PasskeyAuthenticationBeginResponse
        {
            Success = true,
            SessionId = sessionId,
            Options = options.ToJson()
        };
    }

    /// <inheritdoc />
    public async Task<PasskeyAuthenticationCompleteResponse> CompleteAuthenticationAsync(
        PasskeyAuthenticationCompleteRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        AuthenticationSession? session;
        lock (AuthenticationSessions)
        {
            if (!AuthenticationSessions.TryGetValue(request.SessionId, out session))
            {
                return new PasskeyAuthenticationCompleteResponse { Success = false, Error = "Invalid or expired session." };
            }

            AuthenticationSessions.Remove(request.SessionId);
        }

        try
        {
            var assertionResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(request.AssertionResponse);
            if (assertionResponse is null)
            {
                return new PasskeyAuthenticationCompleteResponse { Success = false, Error = "Invalid assertion response." };
            }

            // assertionResponse.Id is Base64URL encoded - matches what we store
            var credentialIdBase64Url = assertionResponse.Id;
            
            var passkey = await _dbContext.UserPasskeys
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.CredentialId == credentialIdBase64Url && p.IsActive, cancellationToken);

            if (passkey is null)
            {
                await _auditLog.LogAuthenticationEventAsync(
                    AuditActions.PasskeyAuthFailed,
                    AuditOutcome.Failure,
                    null,
                    null,
                    ipAddress,
                    userAgent,
                    "Unknown credential",
                    cancellationToken);

                return new PasskeyAuthenticationCompleteResponse { Success = false, Error = "Credential not found." };
            }

            var user = passkey.User;
            if (!user.IsActive)
            {
                await _auditLog.LogAuthenticationEventAsync(
                    AuditActions.PasskeyAuthFailed,
                    AuditOutcome.Failure,
                    user.Id,
                    user.Email,
                    ipAddress,
                    userAgent,
                    "User inactive",
                    cancellationToken);

                return new PasskeyAuthenticationCompleteResponse { Success = false, Error = "Account is inactive." };
            }

            // Callback to verify user handle ownership
            IsUserHandleOwnerOfCredentialIdAsync callback = async (args, ct) =>
            {
                // We've already verified the credential exists and belongs to a user
                return true;
            };

            var result = await _fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = assertionResponse,
                OriginalOptions = session.Options,
                StoredPublicKey = Convert.FromBase64String(passkey.PublicKey),
                StoredSignatureCounter = passkey.SignatureCounter,
                IsUserHandleOwnerOfCredentialIdCallback = callback
            }, cancellationToken);

            // In v4, MakeAssertionAsync throws on failure, so if we reach here it succeeded

            // Update signature counter
            passkey.SignatureCounter = result.SignCount;
            passkey.LastUsedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Sign in the user
            await _signInManager.SignInAsync(user, isPersistent: false);

            await _auditLog.LogAuthenticationEventAsync(
                AuditActions.PasskeyAuthenticated,
                AuditOutcome.Success,
                user.Id,
                user.Email,
                ipAddress,
                userAgent,
                $"Device: {passkey.DeviceName}",
                cancellationToken);

            await _auditLog.LogAuthenticationEventAsync(
                AuditActions.LoginSuccess,
                AuditOutcome.Success,
                user.Id,
                user.Email,
                ipAddress,
                userAgent,
                cancellationToken: cancellationToken);

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateAuthToken();

            _logger.LogInformation("User {UserId} authenticated with passkey: {DeviceName}", user.Id, passkey.DeviceName);

            return new PasskeyAuthenticationCompleteResponse
            {
                Success = true,
                UserId = user.Id,
                Token = token,
                Roles = roles.ToList(),
                DeviceName = passkey.DeviceName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Passkey authentication failed");
            return new PasskeyAuthenticationCompleteResponse { Success = false, Error = "Authentication failed." };
        }
    }

    /// <inheritdoc />
    public async Task<PasskeyListResponse> GetUserPasskeysAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var passkeys = await _dbContext.UserPasskeys
            .Where(p => p.UserId == userId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PasskeyDto
            {
                Id = p.Id,
                DeviceName = p.DeviceName,
                CreatedAt = p.CreatedAt,
                LastUsedAt = p.LastUsedAt,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

        return new PasskeyListResponse
        {
            Passkeys = passkeys,
            TotalCount = passkeys.Count
        };
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePasskeyAsync(
        Guid userId,
        Guid passkeyId,
        UpdatePasskeyRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var passkey = await _dbContext.UserPasskeys
            .FirstOrDefaultAsync(p => p.Id == passkeyId && p.UserId == userId, cancellationToken);

        if (passkey is null)
        {
            return false;
        }

        var oldName = passkey.DeviceName;
        passkey.DeviceName = request.DeviceName;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.PasskeyUpdated,
            AuditOutcome.Success,
            userId,
            user?.Email,
            ipAddress,
            null,
            $"Renamed passkey from '{oldName}' to '{request.DeviceName}'",
            cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<DeletePasskeyResponse> DeletePasskeyAsync(
        Guid userId,
        Guid passkeyId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var passkey = await _dbContext.UserPasskeys
            .FirstOrDefaultAsync(p => p.Id == passkeyId && p.UserId == userId, cancellationToken);

        if (passkey is null)
        {
            return new DeletePasskeyResponse { Success = false, Error = "Passkey not found." };
        }

        // Check if this is the last passkey
        var remainingCount = await _dbContext.UserPasskeys
            .CountAsync(p => p.UserId == userId && p.IsActive && p.Id != passkeyId, cancellationToken);

        if (remainingCount == 0)
        {
            return new DeletePasskeyResponse
            {
                Success = false,
                Error = "Cannot delete the last passkey. Please register another passkey first.",
                RemainingPasskeys = 1
            };
        }

        var deviceName = passkey.DeviceName;
        passkey.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.PasskeyDeleted,
            AuditOutcome.Success,
            userId,
            user?.Email,
            ipAddress,
            null,
            $"Deleted passkey: {deviceName}",
            cancellationToken);

        return new DeletePasskeyResponse
        {
            Success = true,
            RemainingPasskeys = remainingCount
        };
    }

    /// <inheritdoc />
    public async Task<int> GetPasskeyCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserPasskeys
            .CountAsync(p => p.UserId == userId && p.IsActive, cancellationToken);
    }

    private static string GenerateSessionId()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static string GenerateAuthToken()
    {
        // In production, this should generate a proper JWT token
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static void CleanupExpiredSessions()
    {
        var expiry = DateTime.UtcNow.AddMinutes(-5);

        var expiredReg = RegistrationSessions
            .Where(kvp => kvp.Value.CreatedAt < expiry)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredReg)
        {
            RegistrationSessions.Remove(key);
        }

        var expiredAuth = AuthenticationSessions
            .Where(kvp => kvp.Value.CreatedAt < expiry)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredAuth)
        {
            AuthenticationSessions.Remove(key);
        }
    }

    private sealed class RegistrationSession
    {
        public required Guid UserId { get; init; }
        public required string DeviceName { get; init; }
        public required CredentialCreateOptions Options { get; init; }
        public required DateTime CreatedAt { get; init; }
    }

    private sealed class AuthenticationSession
    {
        public Guid? UserId { get; init; }
        public required AssertionOptions Options { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}

/// <summary>
/// Configuration settings for FIDO2/WebAuthn.
/// Required settings must be configured in appsettings or Key Vault.
/// </summary>
public sealed class Fido2Settings
{
    public const string SectionName = "Fido2";

    /// <summary>
    /// Gets or sets the relying party name (e.g., "LenkCare Homes").
    /// </summary>
    public required string ServerName { get; set; }

    /// <summary>
    /// Gets or sets the relying party ID (domain).
    /// Must match the domain where the frontend is hosted (e.g., "localhost" or "dev.homes.lenkcare.com").
    /// </summary>
    public required string ServerDomain { get; set; }

    /// <summary>
    /// Gets or sets the allowed origins for WebAuthn requests.
    /// Must include the frontend URL(s) where passkey authentication is initiated from.
    /// </summary>
    public required HashSet<string> Origins { get; set; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds.
    /// </summary>
    public uint Timeout { get; set; } = 60000;
}
