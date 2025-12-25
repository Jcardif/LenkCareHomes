using LenkCareHomes.Api.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace LenkCareHomes.Api.Authorization;

/// <summary>
/// Authorization requirement that denies access to users with Sysadmin role
/// for PHI (Protected Health Information) operations.
/// </summary>
public sealed class DenyPhiAccessToSysadminRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Handler that checks if the user has the Sysadmin role and denies PHI access.
/// </summary>
public sealed class DenyPhiAccessToSysadminHandler : AuthorizationHandler<DenyPhiAccessToSysadminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DenyPhiAccessToSysadminRequirement requirement)
    {
        // If user is not authenticated, let other handlers deal with it
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        // Check if user has Sysadmin role
        var isSysadmin = context.User.IsInRole(Roles.Sysadmin);
        
        // Check if user also has Admin or Caregiver role (which would grant PHI access)
        var hasPhiRole = context.User.IsInRole(Roles.Admin) || context.User.IsInRole(Roles.Caregiver);

        // If user is only Sysadmin (no PHI roles), deny access
        if (isSysadmin && !hasPhiRole)
        {
            context.Fail(new AuthorizationFailureReason(this, "Sysadmin role cannot access PHI"));
            return Task.CompletedTask;
        }

        // User has PHI access role, succeed
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Attribute to mark controllers or actions as containing PHI,
/// which restricts Sysadmin access.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequirePhiAccessAttribute : AuthorizeAttribute
{
    public RequirePhiAccessAttribute() : base("RequirePhiAccess")
    {
    }
}

/// <summary>
/// Extension methods for configuring PHI access authorization.
/// </summary>
public static class PhiAuthorizationExtensions
{
    /// <summary>
    /// Adds PHI access authorization policies to the service collection.
    /// </summary>
    public static IServiceCollection AddPhiAccessAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, DenyPhiAccessToSysadminHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy("RequirePhiAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new DenyPhiAccessToSysadminRequirement());
            });

        return services;
    }
}
