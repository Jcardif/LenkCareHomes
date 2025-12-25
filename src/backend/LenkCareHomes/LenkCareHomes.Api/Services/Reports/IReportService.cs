using LenkCareHomes.Api.Models.Reports;

namespace LenkCareHomes.Api.Services.Reports;

/// <summary>
///     Service interface for report generation.
/// </summary>
public interface IReportService
{
    /// <summary>
    ///     Generates a client summary report with all care data for the specified date range.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="startDate">Start date of the report period.</param>
    /// <param name="endDate">End date of the report period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated client report data.</returns>
    Task<ClientSummaryReportData?> GetClientSummaryDataAsync(
        Guid clientId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generates a home summary report with client and incident data for the specified date range.
    /// </summary>
    /// <param name="homeId">The home ID.</param>
    /// <param name="startDate">Start date of the report period.</param>
    /// <param name="endDate">End date of the report period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated home report data.</returns>
    Task<HomeSummaryReportData?> GetHomeSummaryDataAsync(
        Guid homeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a client exists.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the client exists.</returns>
    Task<bool> ClientExistsAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a home exists.
    /// </summary>
    /// <param name="homeId">The home ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the home exists.</returns>
    Task<bool> HomeExistsAsync(Guid homeId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the home ID for a client.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The home ID if found.</returns>
    Task<Guid?> GetClientHomeIdAsync(Guid clientId, CancellationToken cancellationToken = default);
}