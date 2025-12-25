using LenkCareHomes.Api.Models.Reports;

namespace LenkCareHomes.Api.Services.Reports;

/// <summary>
/// Service interface for generating PDF reports.
/// </summary>
public interface IPdfReportService
{
    /// <summary>
    /// Generates a PDF document for a client summary report.
    /// </summary>
    /// <param name="data">The aggregated client report data.</param>
    /// <returns>PDF document as byte array.</returns>
    byte[] GenerateClientSummaryPdf(ClientSummaryReportData data);

    /// <summary>
    /// Generates a PDF document for a home summary report.
    /// </summary>
    /// <param name="data">The aggregated home report data.</param>
    /// <returns>PDF document as byte array.</returns>
    byte[] GenerateHomeSummaryPdf(HomeSummaryReportData data);
}
