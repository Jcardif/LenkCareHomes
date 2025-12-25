namespace LenkCareHomes.Api.Domain.Constants;

/// <summary>
/// Defines the available user roles in the LenkCare Homes system.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Admin/Owner role with full system access.
    /// Can manage homes, beds, clients, caregivers, documents, reports, and audit logs.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Caregiver role with limited home-scoped access.
    /// Can view clients, log ADLs/vitals/notes, view permitted documents (no download).
    /// </summary>
    public const string Caregiver = "Caregiver";

    /// <summary>
    /// Developer/Sysadmin role for system maintenance.
    /// Cannot access or modify PHI - only system configuration and audit logs.
    /// </summary>
    public const string Sysadmin = "Sysadmin";

    /// <summary>
    /// Gets all available roles.
    /// </summary>
    public static IReadOnlyList<string> All => [Admin, Caregiver, Sysadmin];

    /// <summary>
    /// Gets roles that have access to PHI (Protected Health Information).
    /// </summary>
    public static IReadOnlyList<string> PhiAccessRoles => [Admin, Caregiver];
}
