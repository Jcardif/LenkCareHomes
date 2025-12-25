using System.Text.Json;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Middleware;
using LenkCareHomes.Api.Services.SyntheticData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LenkCareHomes.Api.Controllers;

/// <summary>
/// Controller for synthetic data operations.
/// Only available in development environment and restricted to Sysadmin role.
/// This controller is protected by multiple layers:
/// 1. DevelopmentOnly attribute - returns 404 in non-dev environments
/// 2. Sysadmin role authorization
/// 3. Service-level environment check
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Sysadmin)]
[DevelopmentOnly]
public sealed class SyntheticDataController : ControllerBase
{
    private readonly ISyntheticDataService _syntheticDataService;
    private readonly ILogger<SyntheticDataController> _logger;

    public SyntheticDataController(
        ISyntheticDataService syntheticDataService,
        ILogger<SyntheticDataController> logger)
    {
        _syntheticDataService = syntheticDataService;
        _logger = logger;
    }

    /// <summary>
    /// Checks if synthetic data operations are available.
    /// </summary>
    /// <returns>Availability status.</returns>
    [HttpGet("available")]
    [ProducesResponseType(typeof(SyntheticDataAvailabilityResponse), StatusCodes.Status200OK)]
    public ActionResult<SyntheticDataAvailabilityResponse> CheckAvailability()
    {
        return Ok(new SyntheticDataAvailabilityResponse
        {
            IsAvailable = _syntheticDataService.IsAvailable,
            Message = _syntheticDataService.IsAvailable
                ? "Synthetic data operations are available."
                : "Synthetic data operations are only available in development environment."
        });
    }

    /// <summary>
    /// Gets current database statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Database statistics.</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(DataStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DataStatistics>> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_syntheticDataService.IsAvailable)
        {
            return Forbid();
        }

        var stats = await _syntheticDataService.GetStatisticsAsync(cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Loads synthetic data into the database.
    /// Only available in development environment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Load result.</returns>
    [HttpPost("load")]
    [ProducesResponseType(typeof(LoadSyntheticDataResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoadSyntheticDataResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoadSyntheticDataResult>> LoadDataAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_syntheticDataService.IsAvailable)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new LoadSyntheticDataResult
            {
                Success = false,
                Error = "Synthetic data operations are only available in development environment."
            });
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        _logger.LogInformation("User {UserId} initiated synthetic data load", currentUserId);

        var result = await _syntheticDataService.LoadDataAsync(
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Loads synthetic data with real-time progress streaming via Server-Sent Events (SSE).
    /// Only available in development environment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SSE stream of progress updates, ending with the final result.</returns>
    [HttpGet("load-stream")]
    [Produces("text/event-stream")]
    public async Task LoadDataStreamAsync(CancellationToken cancellationToken = default)
    {
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.ContentType = "text/event-stream";

        if (!_syntheticDataService.IsAvailable)
        {
            await WriteSSEEventAsync("error", new { error = "Synthetic data operations are only available in development environment." }, cancellationToken);
            return;
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            await WriteSSEEventAsync("error", new { error = "Unauthorized" }, cancellationToken);
            return;
        }

        _logger.LogInformation("User {UserId} initiated synthetic data load with progress streaming", currentUserId);

        var result = await _syntheticDataService.LoadDataWithProgressAsync(
            currentUserId.Value,
            GetClientIpAddress(),
            async progress =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await WriteSSEEventAsync("progress", progress, cancellationToken);
                }
            },
            cancellationToken);

        // Send final result
        await WriteSSEEventAsync("complete", result, cancellationToken);
    }

    private async Task WriteSSEEventAsync<T>(string eventType, T data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await Response.WriteAsync($"event: {eventType}\n", cancellationToken);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Clears all non-system data from the database.
    /// Only available in development environment.
    /// USE WITH EXTREME CAUTION - this is destructive!
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Clear result.</returns>
    [HttpPost("clear")]
    [ProducesResponseType(typeof(ClearDataResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ClearDataResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ClearDataResult>> ClearDataAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_syntheticDataService.IsAvailable)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ClearDataResult
            {
                Success = false,
                Error = "Synthetic data operations are only available in development environment."
            });
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        _logger.LogWarning("User {UserId} initiated data clear operation", currentUserId);

        var result = await _syntheticDataService.ClearDataAsync(
            currentUserId.Value,
            GetClientIpAddress(),
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Clears all data with real-time progress streaming via Server-Sent Events (SSE).
    /// Only available in development environment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SSE stream of progress updates, ending with the final result.</returns>
    [HttpGet("clear-stream")]
    [Produces("text/event-stream")]
    public async Task ClearDataStreamAsync(CancellationToken cancellationToken = default)
    {
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.ContentType = "text/event-stream";

        if (!_syntheticDataService.IsAvailable)
        {
            await WriteSSEEventAsync("error", new { error = "Synthetic data operations are only available in development environment." }, cancellationToken);
            return;
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            await WriteSSEEventAsync("error", new { error = "Unauthorized" }, cancellationToken);
            return;
        }

        _logger.LogWarning("User {UserId} initiated data clear with progress streaming", currentUserId);

        var result = await _syntheticDataService.ClearDataWithProgressAsync(
            currentUserId.Value,
            GetClientIpAddress(),
            async progress =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await WriteSSEEventAsync("progress", progress, cancellationToken);
                }
            },
            cancellationToken);

        // Send final result
        await WriteSSEEventAsync("complete", result, cancellationToken);
    }

    private string? GetClientIpAddress()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}

/// <summary>
/// Response for synthetic data availability check.
/// </summary>
public sealed record SyntheticDataAvailabilityResponse
{
    public bool IsAvailable { get; init; }
    public string Message { get; init; } = "";
}
