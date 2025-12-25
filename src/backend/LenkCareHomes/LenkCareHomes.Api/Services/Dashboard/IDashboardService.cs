using LenkCareHomes.Api.Models.Dashboard;

namespace LenkCareHomes.Api.Services.Dashboard;

/// <summary>
/// Service interface for dashboard statistics operations.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets dashboard statistics for admin users.
    /// </summary>
    Task<AdminDashboardStats> GetAdminDashboardStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dashboard statistics for caregiver users.
    /// </summary>
    Task<CaregiverDashboardStats> GetCaregiverDashboardStatsAsync(
        Guid caregiverId,
        CancellationToken cancellationToken = default);
}
