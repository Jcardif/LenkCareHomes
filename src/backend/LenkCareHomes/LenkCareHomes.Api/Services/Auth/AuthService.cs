using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Models.Auth;
using LenkCareHomes.Api.Services.Audit;
using LenkCareHomes.Api.Services.Email;
using LenkCareHomes.Api.Services.Passkey;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LenkCareHomes.Api.Services.Auth;

/// <summary>
/// Authentication service implementation with passkey-based MFA support.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLog;
    private readonly IEmailService _emailService;
    private readonly IPasskeyService _passkeyService;
    private readonly ILogger<AuthService> _logger;
    private readonly AuthSettings _settings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext dbContext,
        IAuditLogService auditLog,
        IEmailService emailService,
        IPasskeyService passkeyService,
        IOptions<AuthSettings> settings,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _auditLog = auditLog;
        _emailService = emailService;
        _passkeyService = passkeyService;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new LoginResponse { Success = false, Error = "Email and password are required." };
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            await _auditLog.LogAuthenticationEventAsync(
                AuditActions.LoginFailed,
                AuditOutcome.Failure,
                null,
                request.Email,
                ipAddress,
                userAgent,
                "User not found or inactive",
                cancellationToken);

            // Return generic error to prevent user enumeration
            return new LoginResponse { Success = false, Error = "Invalid email or password." };
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            await _auditLog.LogAuthenticationEventAsync(
                AuditActions.LoginFailed,
                AuditOutcome.Failure,
                user.Id,
                user.Email,
                ipAddress,
                userAgent,
                "Invalid password",
                cancellationToken);

            return new LoginResponse { Success = false, Error = "Invalid email or password." };
        }

        // Check if passkey setup is required (first login after invitation or MFA reset)
        if (!user.IsMfaSetupComplete || user.RequiresPasskeySetup)
        {
            var setupRoles = await _userManager.GetRolesAsync(user);
            var passkeySetupToken = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "PasskeySetup");
            return new LoginResponse
            {
                Success = true,
                RequiresPasskeySetup = true,
                UserId = user.Id,
                Email = user.Email,
                PasskeySetupToken = passkeySetupToken,
                IsSysadmin = setupRoles.Contains(Roles.Sysadmin)
            };
        }

        // Check if user has any passkeys registered
        var passkeyCount = await _passkeyService.GetPasskeyCountAsync(user.Id, cancellationToken);
        if (passkeyCount == 0)
        {
            // User completed MFA setup but has no passkeys - needs to register one
            var noPasskeyRoles = await _userManager.GetRolesAsync(user);
            var passkeySetupToken = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "PasskeySetup");
            return new LoginResponse
            {
                Success = true,
                RequiresPasskeySetup = true,
                UserId = user.Id,
                Email = user.Email,
                PasskeySetupToken = passkeySetupToken,
                IsSysadmin = noPasskeyRoles.Contains(Roles.Sysadmin)
            };
        }

        // Passkey authentication is required
        var roles = await _userManager.GetRolesAsync(user);
        return new LoginResponse
        {
            Success = true,
            RequiresPasskey = true,
            Email = user.Email,
            UserId = user.Id,
            IsSysadmin = roles.Contains(Roles.Sysadmin)
        };
    }

    /// <inheritdoc />
    public async Task<MfaVerifyResponse> VerifyBackupCodeAsync(
        BackupCodeVerifyRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Delegate to the Sysadmin-specific verification method
        return await VerifyBackupCodeForSysadminAsync(
            request.UserId,
            request.BackupCode,
            ipAddress,
            userAgent,
            cancellationToken);
    }

    /// <summary>
    /// Verifies a backup code for Sysadmin MFA recovery.
    /// </summary>
    /// <param name="userId">User ID of the Sysadmin.</param>
    /// <param name="backupCode">The backup code to verify.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="userAgent">Client user agent for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>MFA verification response with authentication token if successful.</returns>
    public async Task<MfaVerifyResponse> VerifyBackupCodeForSysadminAsync(
        Guid userId,
        string backupCode,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return new MfaVerifyResponse { Success = false, Error = "User not found." };
        }

        // Verify user is Sysadmin
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(Roles.Sysadmin))
        {
            await _auditLog.LogAuthenticationEventAsync(
                AuditActions.MfaFailed,
                AuditOutcome.Failure,
                user.Id,
                user.Email,
                ipAddress,
                userAgent,
                "Backup code authentication attempted by non-Sysadmin user",
                cancellationToken);

            return new MfaVerifyResponse
            {
                Success = false,
                Error = "Backup code authentication is only available for Sysadmin users."
            };
        }

        if (string.IsNullOrEmpty(user.BackupCodesHash) || user.RemainingBackupCodes <= 0)
        {
            return new MfaVerifyResponse { Success = false, Error = "No backup codes available." };
        }

        var backupCodes = JsonSerializer.Deserialize<List<string>>(user.BackupCodesHash) ?? [];
        var normalizedCode = backupCode.Replace("-", "").ToUpperInvariant();
        var hashedCode = HashBackupCode(normalizedCode);

        if (!backupCodes.Remove(hashedCode))
        {
            await _auditLog.LogAuthenticationEventAsync(
                AuditActions.MfaFailed,
                AuditOutcome.Failure,
                user.Id,
                user.Email,
                ipAddress,
                userAgent,
                "Invalid backup code for Sysadmin",
                cancellationToken);

            return new MfaVerifyResponse { Success = false, Error = "Invalid backup code." };
        }

        // Update backup codes
        user.BackupCodesHash = JsonSerializer.Serialize(backupCodes);
        user.RemainingBackupCodes = backupCodes.Count;
        
        // Disable all existing passkeys for this user
        var existingPasskeys = await _dbContext.UserPasskeys
            .Where(p => p.UserId == userId && p.IsActive)
            .ToListAsync(cancellationToken);
        
        foreach (var passkey in existingPasskeys)
        {
            passkey.IsActive = false;
        }
        
        // Mark user as requiring passkey setup
        user.IsMfaSetupComplete = false;
        user.RequiresPasskeySetup = true;
        await _userManager.UpdateAsync(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        // Generate passkey setup token
        var passkeySetupToken = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "PasskeySetup");

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.BackupCodeUsed,
            AuditOutcome.Success,
            user.Id,
            user.Email,
            ipAddress,
            userAgent,
            $"Sysadmin backup code used for passkey reset. Passkeys disabled: {existingPasskeys.Count}. Remaining codes: {user.RemainingBackupCodes}",
            cancellationToken);

        return new MfaVerifyResponse
        {
            Success = true,
            UserId = user.Id,
            RemainingBackupCodes = user.RemainingBackupCodes,
            RequiresPasskeySetup = true,
            PasskeySetupToken = passkeySetupToken
        };
    }

    /// <inheritdoc />
    public async Task<MfaSetupResponse> SetupMfaAsync(Guid userId, string passkeySetupToken, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        // Validate the temporary token
        var isValid = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "PasskeySetup", passkeySetupToken);
        if (!isValid)
        {
            throw new UnauthorizedAccessException("Invalid or expired setup token.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var isSysadmin = roles.Contains(Roles.Sysadmin);

        List<string>? backupCodes = null;

        // Only generate backup codes for Sysadmin users who don't already have them
        // (This preserves existing codes during passkey reset flow)
        if (isSysadmin)
        {
            // Check if user already has backup codes (passkey reset case)
            if (!string.IsNullOrEmpty(user.BackupCodesHash) && user.RemainingBackupCodes > 0)
            {
                // User is resetting passkey - keep their existing backup codes
                _logger.LogInformation("Sysadmin user {UserId} already has {Count} backup codes, preserving them", userId, user.RemainingBackupCodes);
            }
            else
            {
                // Fresh setup - generate new backup codes
                backupCodes = GenerateBackupCodes();
                var hashedCodes = backupCodes.Select(c => HashBackupCode(c.Replace("-", ""))).ToList();
                user.BackupCodesHash = JsonSerializer.Serialize(hashedCodes);
                user.RemainingBackupCodes = backupCodes.Count;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Generated backup codes for Sysadmin user {UserId}", userId);
            }
        }
        else
        {
            // Clear any existing backup codes for non-Sysadmin users
            user.BackupCodesHash = null;
            user.RemainingBackupCodes = 0;
            await _userManager.UpdateAsync(user);
        }

        // Check if user has already completed their profile
        var hasProfileCompleted = !string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName);

        return new MfaSetupResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            BackupCodes = backupCodes,
            HasBackupCodes = isSysadmin,
            HasProfileCompleted = hasProfileCompleted
        };
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmMfaSetupAsync(MfaSetupConfirmRequest request, string passkeySetupToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("MFA setup confirmation requested for user {UserId}", request.UserId);

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            _logger.LogWarning("MFA setup confirmation failed: user {UserId} not found", request.UserId);
            return false;
        }

        // Validate the temporary token
        var isValid = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "PasskeySetup", passkeySetupToken);
        if (!isValid)
        {
            _logger.LogWarning("MFA setup confirmation failed: invalid token for user {UserId}", request.UserId);
            return false;
        }

        // Check that user has at least one passkey registered
        var passkeyCount = await _passkeyService.GetPasskeyCountAsync(request.UserId, cancellationToken);
        if (passkeyCount == 0)
        {
            _logger.LogWarning("MFA setup confirmation failed: no passkeys registered for user {UserId}", request.UserId);
            return false;
        }

        // For Sysadmin users, verify they've acknowledged backup codes
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(Roles.Sysadmin) && !request.BackupCodesSaved)
        {
            _logger.LogWarning("MFA setup confirmation failed: Sysadmin {UserId} has not confirmed backup codes", request.UserId);
            return false;
        }

        user.IsMfaSetupComplete = true;
        user.RequiresPasskeySetup = false;
        user.TwoFactorEnabled = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.MfaSetup,
            AuditOutcome.Success,
            user.Id,
            user.Email,
            null,
            null,
            $"Passkey-based MFA setup completed. Passkeys registered: {passkeyCount}",
            cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task LogoutAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.Logout,
            AuditOutcome.Success,
            userId,
            user?.Email,
            ipAddress,
            userAgent,
            cancellationToken: cancellationToken);

        await _signInManager.SignOutAsync();
    }

    /// <inheritdoc />
    public async Task RequestPasswordResetAsync(
        PasswordResetRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userManager.FindByEmailAsync(request.Email);

        // Always log the attempt for security auditing
        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.PasswordResetRequested,
            user is not null ? AuditOutcome.Success : AuditOutcome.Failure,
            user?.Id,
            request.Email,
            ipAddress,
            null,
            user is null ? "User not found" : null,
            cancellationToken);

        // Don't reveal whether user exists
        if (user is null || !user.IsActive)
        {
            return;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = $"{_settings.FrontendBaseUrl}/auth/forgot-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

        await _emailService.SendPasswordResetEmailAsync(
            user.Email!,
            user.FirstName,
            resetLink,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ResetPasswordAsync(
        PasswordResetConfirmRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Extract email from token (token format includes user ID)
        // In production, token should be properly validated
        var users = _userManager.Users.ToList();
        ApplicationUser? targetUser = null;

        foreach (var user in users)
        {
            var isValidToken = await _userManager.VerifyUserTokenAsync(
                user,
                _userManager.Options.Tokens.PasswordResetTokenProvider,
                "ResetPassword",
                request.Token);

            if (isValidToken)
            {
                targetUser = user;
                break;
            }
        }

        if (targetUser is null)
        {
            return false;
        }

        var result = await _userManager.ResetPasswordAsync(targetUser, request.Token, request.NewPassword);

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.PasswordReset,
            result.Succeeded ? AuditOutcome.Success : AuditOutcome.Failure,
            targetUser.Id,
            targetUser.Email,
            ipAddress,
            null,
            result.Succeeded ? null : string.Join(", ", result.Errors.Select(e => e.Description)),
            cancellationToken);

        return result.Succeeded;
    }

    /// <inheritdoc />
    public async Task<InvitationAcceptResponse> AcceptInvitationAsync(
        InvitationAcceptRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = _userManager.Users.FirstOrDefault(u => u.InvitationToken == request.InvitationToken);
        if (user is null)
        {
            return new InvitationAcceptResponse { Success = false, Error = "Invalid invitation token." };
        }

        if (user.InvitationAccepted)
        {
            return new InvitationAcceptResponse { Success = false, Error = "Invitation has already been used." };
        }

        if (user.InvitationExpiresAt < DateTime.UtcNow)
        {
            return new InvitationAcceptResponse { Success = false, Error = "Invitation has expired." };
        }

        // Set the user's password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.Password);

        if (!result.Succeeded)
        {
            return new InvitationAcceptResponse
            {
                Success = false,
                Error = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        // Mark invitation as accepted and set up for passkey registration
        user.InvitationAccepted = true;
        user.InvitationToken = null;
        user.EmailConfirmed = true;
        user.RequiresPasskeySetup = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.InvitationAccepted,
            AuditOutcome.Success,
            user.Id,
            user.Email,
            ipAddress,
            null,
            cancellationToken: cancellationToken);

        // Set up MFA (generates backup codes for Sysadmin only)
        var passkeySetupToken = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, "PasskeySetup");
        var mfaSetup = await SetupMfaAsync(user.Id, passkeySetupToken, cancellationToken);

        return new InvitationAcceptResponse
        {
            Success = true,
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            MfaSetup = mfaSetup,
            PasskeySetupToken = passkeySetupToken
        };
    }

    /// <inheritdoc />
    public async Task<bool> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        string passkeySetupToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        // Validate the temporary token
        var isValid = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "PasskeySetup", passkeySetupToken);
        if (!isValid)
        {
            _logger.LogWarning("Profile update failed: invalid token for user {UserId}", userId);
            return false;
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("Profile updated for user {UserId}", userId);
        }
        else
        {
            _logger.LogWarning("Failed to update profile for user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return result.Succeeded;
    }

    /// <inheritdoc />
    public async Task<CompleteSetupResponse> CompleteSetupAsync(
        Guid userId,
        string passkeySetupToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        // Validate the temporary token
        var isValid = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "PasskeySetup", passkeySetupToken);
        if (!isValid)
        {
            return new CompleteSetupResponse
            {
                Success = false,
                Error = "Invalid or expired setup token."
            };
        }

        if (!user.IsMfaSetupComplete)
        {
            return new CompleteSetupResponse
            {
                Success = false,
                Error = "MFA setup is not complete."
            };
        }

        // Verify user has at least one passkey
        var passkeyCount = await _passkeyService.GetPasskeyCountAsync(userId, cancellationToken);
        if (passkeyCount == 0)
        {
            return new CompleteSetupResponse
            {
                Success = false,
                Error = "At least one passkey must be registered to complete setup."
            };
        }

        if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName))
        {
            return new CompleteSetupResponse
            {
                Success = false,
                Error = "Profile is not complete. Please provide your first and last name."
            };
        }

        // Mark user as fully set up
        user.IsActive = true;
        user.RequiresPasskeySetup = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Sign in the user to establish the authentication cookie
        await _signInManager.SignInAsync(user, isPersistent: false);

        // Log the setup completion
        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.AccountSetupCompleted,
            AuditOutcome.Success,
            user.Id,
            user.Email,
            ipAddress,
            userAgent,
            $"Account setup completed with {passkeyCount} passkey(s)",
            cancellationToken);

        // Generate auth token for auto-login
        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateAuthToken(user);

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.LoginSuccess,
            AuditOutcome.Success,
            user.Id,
            user.Email,
            ipAddress,
            userAgent,
            "Auto-login after setup completion",
            cancellationToken);

        return new CompleteSetupResponse
        {
            Success = true,
            UserId = user.Id,
            Token = token,
            Roles = roles.ToList(),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Roles = roles.ToList()
            }
        };
    }

    private static List<string> GenerateBackupCodes()
    {
        var codes = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToUpperInvariant();
            codes.Add($"{code[..4]}-{code[4..]}");
        }
        return codes;
    }

    private static string HashBackupCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }

    private static string GenerateAuthToken(ApplicationUser user)
    {
        // In production, this should generate a proper JWT token
        // For now, return a placeholder that the frontend can use
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    /// <inheritdoc />
    public async Task<CurrentUserResponse> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new CurrentUserResponse
            {
                Success = false,
                Error = "User not found."
            };
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new CurrentUserResponse
        {
            Success = true,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Roles = roles.ToList(),
                TourCompleted = user.TourCompleted
            }
        };
    }

    /// <inheritdoc />
    public async Task<RegenerateBackupCodesResponse> RegenerateBackupCodesAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            _logger.LogWarning("Regenerate backup codes failed: user {UserId} not found", userId);
            return new RegenerateBackupCodesResponse
            {
                Success = false,
                Error = "User not found."
            };
        }

        // Only Sysadmins can have backup codes
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(Roles.Sysadmin))
        {
            _logger.LogWarning("Regenerate backup codes failed: user {UserId} is not a Sysadmin", userId);
            return new RegenerateBackupCodesResponse
            {
                Success = false,
                Error = "Only Sysadmin users can have backup codes."
            };
        }

        // Generate new backup codes (invalidates all previous ones)
        var backupCodes = GenerateBackupCodes();
        var hashedCodes = backupCodes.Select(c => HashBackupCode(c.Replace("-", ""))).ToList();
        user.BackupCodesHash = JsonSerializer.Serialize(hashedCodes);
        user.RemainingBackupCodes = backupCodes.Count;
        await _userManager.UpdateAsync(user);

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.MfaSetup,
            AuditOutcome.Success,
            user.Id,
            user.Email,
            ipAddress,
            null,
            "Backup codes regenerated. All previous codes invalidated.",
            cancellationToken);

        _logger.LogInformation("Backup codes regenerated for Sysadmin user {UserId}", userId);

        return new RegenerateBackupCodesResponse
        {
            Success = true,
            BackupCodes = backupCodes
        };
    }
}

/// <summary>
/// Configuration settings for authentication.
/// </summary>
public sealed class AuthSettings
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Gets or sets the frontend base URL for generating links.
    /// </summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>
    /// Gets or sets the invitation token expiration in hours.
    /// </summary>
    public int InvitationExpirationHours { get; set; } = 48;

    /// <summary>
    /// Gets or sets the password reset token expiration in hours.
    /// </summary>
    public int PasswordResetExpirationHours { get; set; } = 1;
}
