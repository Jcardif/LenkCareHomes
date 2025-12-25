using System.Security.Cryptography;
using LenkCareHomes.Api.Data;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Models.Users;
using LenkCareHomes.Api.Services.Audit;
using LenkCareHomes.Api.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LenkCareHomes.Api.Services.Users;

/// <summary>
/// User management service implementation.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLog;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserService> _logger;
    private readonly Auth.AuthSettings _authSettings;

    public UserService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IAuditLogService auditLog,
        IEmailService emailService,
        IOptions<Auth.AuthSettings> authSettings,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _auditLog = auditLog;
        _emailService = emailService;
        _authSettings = authSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(MapToDto(user, roles));
        }

        return userDtos;
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return MapToDto(user, roles);
    }

    /// <inheritdoc />
    public async Task<InviteUserResponse> InviteUserAsync(
        InviteUserRequest request,
        Guid invitedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate role
        if (!Roles.All.Contains(request.Role))
        {
            return new InviteUserResponse { Success = false, Error = $"Invalid role: {request.Role}" };
        }

        // Validate home assignments for caregivers
        if (request.Role == Roles.Caregiver)
        {
            if (request.HomeIds is null || request.HomeIds.Count == 0)
            {
                return new InviteUserResponse { Success = false, Error = "Caregivers must be assigned to at least one home." };
            }

            // Validate all home IDs exist and are active
            var validHomeIds = await _dbContext.Homes
                .Where(h => request.HomeIds.Contains(h.Id) && h.IsActive)
                .Select(h => h.Id)
                .ToListAsync(cancellationToken);

            if (validHomeIds.Count != request.HomeIds.Count)
            {
                return new InviteUserResponse { Success = false, Error = "One or more selected homes are invalid or inactive." };
            }
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return new InviteUserResponse { Success = false, Error = "A user with this email already exists." };
        }

        // Generate invitation token
        var invitationToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // Create user
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            InvitationToken = invitationToken,
            InvitationExpiresAt = DateTime.UtcNow.AddHours(_authSettings.InvitationExpirationHours),
            InvitationAccepted = false,
            IsMfaSetupComplete = false,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            return new InviteUserResponse
            {
                Success = false,
                Error = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        // Assign role
        await _userManager.AddToRoleAsync(user, request.Role);

        // Assign homes for caregivers
        if (request.Role == Roles.Caregiver && request.HomeIds is not null)
        {
            foreach (var homeId in request.HomeIds)
            {
                var assignment = new CaregiverHomeAssignment
                {
                    UserId = user.Id,
                    HomeId = homeId,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _dbContext.CaregiverHomeAssignments.Add(assignment);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Log home assignments
            await _auditLog.LogPhiAccessAsync(
                AuditActions.CaregiverHomeAssign,
                invitedById,
                user.Email ?? string.Empty,
                "CaregiverHomeAssignment",
                user.Id.ToString(),
                AuditOutcome.Success,
                ipAddress,
                $"Assigned caregiver to {request.HomeIds.Count} home(s) during invitation",
                cancellationToken);
        }

        // Send invitation email
        if (string.IsNullOrEmpty(user.Email))
        {
            throw new InvalidOperationException("User email is required to send invitation");
        }

        var invitationLink = $"{_authSettings.FrontendBaseUrl}/auth/accept-invitation?token={Uri.EscapeDataString(invitationToken)}";
        await _emailService.SendInvitationEmailAsync(
            user.Email,
            user.FirstName,
            invitationLink,
            cancellationToken);

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.UserInvited,
            AuditOutcome.Success,
            invitedById,
            null,
            ipAddress,
            null,
            $"Invited user {user.Email} with role {request.Role}",
            cancellationToken);

        return new InviteUserResponse { Success = true, UserId = user.Id };
    }

    /// <inheritdoc />
    public async Task<ResendInvitationResponse> ResendInvitationAsync(
        Guid userId,
        Guid resentById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new ResendInvitationResponse { Success = false, Error = "User not found." };
        }

        // Check if user has already accepted invitation
        if (user.InvitationAccepted)
        {
            return new ResendInvitationResponse 
            { 
                Success = false, 
                Error = "User has already accepted their invitation." 
            };
        }

        // Generate a new invitation token
        var newInvitationToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.InvitationToken = newInvitationToken;
        user.InvitationExpiresAt = DateTime.UtcNow.AddHours(_authSettings.InvitationExpirationHours);
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return new ResendInvitationResponse 
            { 
                Success = false, 
                Error = "Failed to update invitation token." 
            };
        }

        // Send invitation email
        if (string.IsNullOrEmpty(user.Email))
        {
            return new ResendInvitationResponse { Success = false, Error = "User has no email address." };
        }

        var invitationLink = $"{_authSettings.FrontendBaseUrl}/auth/accept-invitation?token={Uri.EscapeDataString(newInvitationToken)}";
        await _emailService.SendInvitationEmailAsync(
            user.Email,
            user.FirstName,
            invitationLink,
            cancellationToken);

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.UserInvited,
            AuditOutcome.Success,
            resentById,
            null,
            ipAddress,
            null,
            $"Resent invitation email to user {user.Email}",
            cancellationToken);

        return new ResendInvitationResponse 
        { 
            Success = true, 
            Message = "Invitation email resent successfully." 
        };
    }

    /// <inheritdoc />
    public async Task<UserDto?> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        Guid updatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            user.FirstName = request.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            user.LastName = request.LastName;
        }

        if (request.PhoneNumber is not null)
        {
            user.PhoneNumber = request.PhoneNumber;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.UserUpdated,
            AuditOutcome.Success,
            updatedById,
            null,
            ipAddress,
            null,
            $"Updated user {user.Email}",
            cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        return MapToDto(user, roles);
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateUserAsync(
        Guid userId,
        Guid deactivatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.UserDeactivated,
            AuditOutcome.Success,
            deactivatedById,
            null,
            ipAddress,
            null,
            $"Deactivated user {user.Email}",
            cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ReactivateUserAsync(
        Guid userId,
        Guid reactivatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.UserReactivated,
            AuditOutcome.Success,
            reactivatedById,
            null,
            ipAddress,
            null,
            $"Reactivated user {user.Email}",
            cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<MfaResetResponse> ResetUserMfaAsync(
        MfaResetRequest request,
        Guid resetById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return new MfaResetResponse { Success = false, Error = "User not found." };
        }

        // Check that the requesting user is a Sysadmin
        var resetByUser = await _userManager.FindByIdAsync(resetById.ToString());
        if (resetByUser is null)
        {
            return new MfaResetResponse { Success = false, Error = "Requesting user not found." };
        }

        var resetByRoles = await _userManager.GetRolesAsync(resetByUser);
        if (!resetByRoles.Contains(Roles.Sysadmin))
        {
            _logger.LogWarning(
                "Unauthorized MFA reset attempt by {UserId} for user {TargetUserId}",
                resetById,
                request.UserId);

            await _auditLog.LogAuthenticationEventAsync(
                AuditActions.MfaReset,
                AuditOutcome.Failure,
                resetById,
                resetByUser.Email,
                ipAddress,
                null,
                $"Unauthorized attempt to reset MFA for {user.Email}",
                cancellationToken);

            return new MfaResetResponse { Success = false, Error = "Only Sysadmins can reset user MFA." };
        }

        // Users cannot reset their own MFA
        if (request.UserId == resetById)
        {
            return new MfaResetResponse { Success = false, Error = "Users cannot reset their own MFA." };
        }

        // Count and remove all user passkeys
        var passkeys = await _dbContext.UserPasskeys
            .Where(p => p.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        int passkeysRemoved = passkeys.Count;

        if (passkeys.Count > 0)
        {
            _dbContext.UserPasskeys.RemoveRange(passkeys);
        }

        // Reset user MFA state
        await _userManager.ResetAuthenticatorKeyAsync(user);
        user.IsMfaSetupComplete = false;
        user.TwoFactorEnabled = false;
        user.BackupCodesHash = null;
        user.RemainingBackupCodes = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send notification email to the affected user
        if (user.Email is not null)
        {
            await _emailService.SendMfaResetEmailAsync(
                user.Email,
                user.FirstName,
                cancellationToken);
        }

        // Comprehensive audit logging with all HIPAA-required details
        var auditDetails = $"Reset MFA for user {user.Email}. " +
                           $"Passkeys removed: {passkeysRemoved}. " +
                           $"Reason: {request.Reason}. " +
                           $"Verification method: {request.VerificationMethod}." +
                           (request.Notes is not null ? $" Notes: {request.Notes}" : "");

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.MfaReset,
            AuditOutcome.Success,
            resetById,
            resetByUser.Email,
            ipAddress,
            null,
            auditDetails,
            cancellationToken);

        _logger.LogInformation(
            "MFA reset completed for user {UserId} by Sysadmin {ResetById}. Passkeys removed: {PasskeysRemoved}",
            request.UserId,
            resetById,
            passkeysRemoved);

        return new MfaResetResponse
        {
            Success = true,
            PasskeysRemoved = passkeysRemoved,
            Message = $"MFA reset successfully. {passkeysRemoved} passkey(s) removed. User will need to set up new authentication."
        };
    }

    /// <inheritdoc />
    public async Task<bool> AssignRoleAsync(
        Guid userId,
        string role,
        Guid assignedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!Roles.All.Contains(role))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return false;
        }

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.RoleAssigned,
            AuditOutcome.Success,
            assignedById,
            null,
            ipAddress,
            null,
            $"Assigned role {role} to user {user.Email}",
            cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveRoleAsync(
        Guid userId,
        string role,
        Guid removedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return false;
        }

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.RoleRemoved,
            AuditOutcome.Success,
            removedById,
            null,
            ipAddress,
            null,
            $"Removed role {role} from user {user.Email}",
            cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(
        Guid userId,
        Guid deletedById,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        // Prevent self-deletion
        if (userId == deletedById)
        {
            _logger.LogWarning("User {UserId} attempted to delete themselves", userId);
            return false;
        }

        var userEmail = user.Email;
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to delete user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }

        await _auditLog.LogAuthenticationEventAsync(
            AuditActions.UserDeleted,
            AuditOutcome.Success,
            deletedById,
            null,
            ipAddress,
            null,
            $"Deleted user {userEmail}",
            cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> GetTourCompletedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user?.TourCompleted ?? false;
    }

    /// <inheritdoc />
    public async Task SetTourCompletedAsync(Guid userId, bool completed, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            _logger.LogWarning("Attempted to set tour status for non-existent user {UserId}", userId);
            return;
        }

        user.TourCompleted = completed;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    private static UserDto MapToDto(ApplicationUser user, IList<string> roles) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FirstName = user.FirstName,
        LastName = user.LastName,
        IsActive = user.IsActive,
        IsMfaSetupComplete = user.IsMfaSetupComplete,
        InvitationAccepted = user.InvitationAccepted,
        Roles = roles.ToList(),
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}
