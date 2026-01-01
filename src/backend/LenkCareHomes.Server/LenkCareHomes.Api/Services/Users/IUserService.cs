using LenkCareHomes.Api.Models.Users;

namespace LenkCareHomes.Api.Services.Users;

/// <summary>
///     Service interface for user management operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    ///     Gets all users.
    /// </summary>
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a user by ID.
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invites a new user to the system.
    /// </summary>
    Task<InviteUserResponse> InviteUserAsync(
        InviteUserRequest request,
        Guid invitedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resends an invitation email to a user who hasn't accepted yet.
    ///     Generates a new invitation token and sends a fresh email.
    /// </summary>
    /// <param name="userId">ID of the user to resend invitation to.</param>
    /// <param name="resentById">ID of the admin/sysadmin resending the invitation.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<ResendInvitationResponse> ResendInvitationAsync(
        Guid userId,
        Guid resentById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates a user's information.
    /// </summary>
    Task<UserDto?> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        Guid updatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deactivates a user account.
    /// </summary>
    Task<bool> DeactivateUserAsync(
        Guid userId,
        Guid deactivatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reactivates a user account.
    /// </summary>
    Task<bool> ReactivateUserAsync(
        Guid userId,
        Guid reactivatedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets a user's MFA (passkey authentication).
    ///     This removes all passkeys and backup codes, requiring the user to set up new authentication.
    ///     Only Sysadmins can perform this action - documented with reason and verification method.
    /// </summary>
    /// <param name="request">Reset request with reason and verification details.</param>
    /// <param name="resetById">ID of the Sysadmin performing the reset.</param>
    /// <param name="ipAddress">Client IP address for audit logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with details.</returns>
    Task<MfaResetResponse> ResetUserMfaAsync(
        MfaResetRequest request,
        Guid resetById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Assigns a role to a user.
    /// </summary>
    Task<bool> AssignRoleAsync(
        Guid userId,
        string role,
        Guid assignedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes a role from a user.
    /// </summary>
    Task<bool> RemoveRoleAsync(
        Guid userId,
        string role,
        Guid removedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Permanently deletes a user from the system.
    /// </summary>
    Task<bool> DeleteUserAsync(
        Guid userId,
        Guid deletedById,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the tour completed status for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has completed the onboarding tour.</returns>
    Task<bool> GetTourCompletedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets the tour completed status for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="completed">Whether the tour is completed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetTourCompletedAsync(Guid userId, bool completed, CancellationToken cancellationToken = default);
}