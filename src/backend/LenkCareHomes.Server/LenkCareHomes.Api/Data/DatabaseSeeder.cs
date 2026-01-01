using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LenkCareHomes.Api.Data;

/// <summary>
///     Service for seeding initial data into the database.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    ///     Seeds roles and initial admin user into the database.
    /// </summary>
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Apply pending migrations
        await context.Database.MigrateAsync();

        // Seed roles
        await SeedRolesAsync(roleManager, logger);

        // Seed initial admin user
        await SeedAdminUserAsync(userManager, configuration, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        var roles = new[]
        {
            new ApplicationRole(Roles.Admin)
            {
                Description =
                    "Administrator with full system access. Can manage homes, beds, clients, caregivers, documents, reports, and audit logs.",
                HasPhiAccess = true
            },
            new ApplicationRole(Roles.Caregiver)
            {
                Description =
                    "Caregiver with limited home-scoped access. Can view clients, log ADLs/vitals/notes, view permitted documents (no download).",
                HasPhiAccess = true
            },
            new ApplicationRole(Roles.Sysadmin)
            {
                Description =
                    "System administrator for maintenance only. Cannot access or modify PHI - only system configuration and audit logs.",
                HasPhiAccess = false
            }
        };

        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role.Name!))
            {
                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                    logger.LogInformation("Created role: {RoleName}", role.Name);
                else
                    logger.LogError("Failed to create role {RoleName}: {Errors}",
                        role.Name,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
            }
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        // Get admin credentials from configuration
        var adminEmail = configuration["SeedAdmin:Email"];
        var adminPassword = configuration["SeedAdmin:Password"];
        var adminFirstName = configuration["SeedAdmin:FirstName"] ?? "System";
        var adminLastName = configuration["SeedAdmin:LastName"] ?? "Administrator";

        // Skip if not configured (for non-development environments)
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "Initial admin user not configured. Set SeedAdmin:Email and SeedAdmin:Password in configuration.");
            return;
        }

        // Check if admin user already exists
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is not null)
        {
            logger.LogInformation("Admin user already exists: {Email}", adminEmail);
            return;
        }

        // Create admin user
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = adminFirstName,
            LastName = adminLastName,
            EmailConfirmed = true,
            InvitationAccepted = true,
            IsMfaSetupComplete = false, // Will need to set up MFA on first login
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        // Assign admin role
        await userManager.AddToRoleAsync(adminUser, Roles.Admin);
        logger.LogInformation("Created initial admin user: {Email}", adminEmail);
    }
}