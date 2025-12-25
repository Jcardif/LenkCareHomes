using LenkCareHomes.Api.Models.CareLog;

namespace LenkCareHomes.Api.Services.CareLog;

/// <summary>
/// Service interface for client care timeline operations.
/// </summary>
public interface ITimelineService
{
    /// <summary>
    /// Gets a unified timeline of all care activities for a client.
    /// </summary>
    Task<TimelineResponse> GetClientTimelineAsync(
        Guid clientId,
        TimelineQueryParams queryParams,
        CancellationToken cancellationToken = default);
}
