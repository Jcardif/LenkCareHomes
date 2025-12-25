using System.Security.Claims;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Models.Reports;
using LenkCareHomes.Api.Services.Audit;
using LenkCareHomes.Api.Services.Caregivers;
using LenkCareHomes.Api.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
///     Controller for report generation and download operations.
///     Admin only - reports contain PHI data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public sealed class ReportsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ICaregiverService _caregiverService;
    private readonly ILogger<ReportsController> _logger;
    private readonly IPdfReportService _pdfReportService;
    private readonly IReportService _reportService;

    public ReportsController(
        IReportService reportService,
        IPdfReportService pdfReportService,
        ICaregiverService caregiverService,
        IAuditLogService auditLogService,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _pdfReportService = pdfReportService;
        _caregiverService = caregiverService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    ///     Generates a client summary report PDF.
    /// </summary>
    /// <param name="request">The client report request with date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF file download.</returns>
    [HttpPost("client")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReportGenerationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ReportGenerationResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateClientReportAsync(
        [FromBody] GenerateClientReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        var ipAddress = GetClientIpAddress();

        // Validate request
        if (request.StartDate > request.EndDate)
            return BadRequest(ReportGenerationResponse.Failure("Start date must be before end date."));

        if (request.EndDate > DateTime.UtcNow) request = request with { EndDate = DateTime.UtcNow };

        // Check if client exists
        if (!await _reportService.ClientExistsAsync(request.ClientId, cancellationToken))
            return NotFound(ReportGenerationResponse.Failure("Client not found."));

        _logger.LogInformation(
            "Generating client report for {ClientId} from {Start} to {End} by user {UserId}",
            request.ClientId, request.StartDate, request.EndDate, currentUserId);

        try
        {
            // Get aggregated data
            var reportData = await _reportService.GetClientSummaryDataAsync(
                request.ClientId,
                request.StartDate,
                request.EndDate,
                cancellationToken);

            if (reportData is null)
                return NotFound(ReportGenerationResponse.Failure("Failed to retrieve client data."));

            // Generate PDF
            var pdfBytes = _pdfReportService.GenerateClientSummaryPdf(reportData);

            // Log audit trail
            await _auditLogService.LogPhiAccessAsync(
                "ReportGenerated",
                currentUserId ?? Guid.Empty,
                userEmail ?? "Unknown",
                "Client",
                request.ClientId.ToString(),
                "Success",
                ipAddress,
                $"Generated client summary report for period {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}",
                cancellationToken);

            // Return PDF file
            var fileName =
                $"ClientReport_{reportData.Client.FullName.Replace(" ", "_")}_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}.pdf";

            _logger.LogInformation(
                "Client report generated successfully for {ClientId}, size: {Size} bytes",
                request.ClientId, pdfBytes.Length);

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating client report for {ClientId}", request.ClientId);

            await _auditLogService.LogPhiAccessAsync(
                "ReportGenerationFailed",
                currentUserId ?? Guid.Empty,
                userEmail ?? "Unknown",
                "Client",
                request.ClientId.ToString(),
                "Failed",
                ipAddress,
                $"Failed to generate report: {ex.Message}",
                cancellationToken);

            return BadRequest(ReportGenerationResponse.Failure("Failed to generate report. Please try again."));
        }
    }

    /// <summary>
    ///     Generates a home summary report PDF.
    /// </summary>
    /// <param name="request">The home report request with date range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF file download.</returns>
    [HttpPost("home")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReportGenerationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ReportGenerationResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateHomeReportAsync(
        [FromBody] GenerateHomeReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        var ipAddress = GetClientIpAddress();

        // Validate request
        if (request.StartDate > request.EndDate)
            return BadRequest(ReportGenerationResponse.Failure("Start date must be before end date."));

        if (request.EndDate > DateTime.UtcNow) request = request with { EndDate = DateTime.UtcNow };

        // Check if home exists
        if (!await _reportService.HomeExistsAsync(request.HomeId, cancellationToken))
            return NotFound(ReportGenerationResponse.Failure("Home not found."));

        _logger.LogInformation(
            "Generating home report for {HomeId} from {Start} to {End} by user {UserId}",
            request.HomeId, request.StartDate, request.EndDate, currentUserId);

        try
        {
            // Get aggregated data
            var reportData = await _reportService.GetHomeSummaryDataAsync(
                request.HomeId,
                request.StartDate,
                request.EndDate,
                cancellationToken);

            if (reportData is null) return NotFound(ReportGenerationResponse.Failure("Failed to retrieve home data."));

            // Generate PDF
            var pdfBytes = _pdfReportService.GenerateHomeSummaryPdf(reportData);

            // Log audit trail
            await _auditLogService.LogPhiAccessAsync(
                "ReportGenerated",
                currentUserId ?? Guid.Empty,
                userEmail ?? "Unknown",
                "Home",
                request.HomeId.ToString(),
                "Success",
                ipAddress,
                $"Generated home summary report for period {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}",
                cancellationToken);

            // Return PDF file
            var fileName =
                $"HomeReport_{reportData.Home.Name.Replace(" ", "_")}_{request.StartDate:yyyyMMdd}_{request.EndDate:yyyyMMdd}.pdf";

            _logger.LogInformation(
                "Home report generated successfully for {HomeId}, size: {Size} bytes",
                request.HomeId, pdfBytes.Length);

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating home report for {HomeId}", request.HomeId);

            await _auditLogService.LogPhiAccessAsync(
                "ReportGenerationFailed",
                currentUserId ?? Guid.Empty,
                userEmail ?? "Unknown",
                "Home",
                request.HomeId.ToString(),
                "Failed",
                ipAddress,
                $"Failed to generate report: {ex.Message}",
                cancellationToken);

            return BadRequest(ReportGenerationResponse.Failure("Failed to generate report. Please try again."));
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId)) return userId;
        return null;
    }

    private string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    private string? GetClientIpAddress()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor)) return forwardedFor.Split(',').First().Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}