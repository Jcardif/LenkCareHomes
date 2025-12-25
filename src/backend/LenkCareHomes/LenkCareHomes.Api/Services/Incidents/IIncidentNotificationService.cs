using LenkCareHomes.Api.Domain.Enums;

namespace LenkCareHomes.Api.Services.Incidents;

/// <summary>
///     Service interface for incident notifications.
/// </summary>
public interface IIncidentNotificationService
{
    /// <summary>
    ///     Sends notification to all admins about a new incident.
    /// </summary>
    Task NotifyAdminsOfNewIncidentAsync(
        Guid incidentId,
        string clientName,
        string homeName,
        IncidentType incidentType,
        string reportedByName,
        CancellationToken cancellationToken = default);
}