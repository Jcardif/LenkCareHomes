using System.Diagnostics;
using System.Security.Claims;
using LenkCareHomes.Api.Domain.Constants;
using LenkCareHomes.Api.Domain.Entities;
using LenkCareHomes.Api.Services.Audit;

namespace LenkCareHomes.Api.Middleware;

/// <summary>
/// Middleware that logs all API requests to the audit log.
/// Captures request details, user information, and response status.
/// </summary>
public sealed class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    /// <summary>
    /// Paths that should not be logged (health checks, static files, etc.)
    /// </summary>
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/alive",
        "/ready",
        "/favicon.ico",
        "/openapi"
    };

    /// <summary>
    /// Paths that involve PHI (Protected Health Information) access.
    /// These get special logging treatment.
    /// </summary>
    private static readonly HashSet<string> PhiPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/clients",
        "/api/residents",
        "/api/documents",
        "/api/adl",
        "/api/vitals",
        "/api/notes",
        "/api/incidents",
        "/api/medications"
    };

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip excluded paths
        if (ShouldExcludePath(path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var correlationId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;

        // Capture request info before processing
        var requestInfo = CaptureRequestInfo(context, correlationId);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Log the request after processing
            await LogRequestAsync(
                context,
                auditLogService,
                requestInfo,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static bool ShouldExcludePath(string path)
    {
        return ExcludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPhiPath(string path)
    {
        return PhiPaths.Any(phiPath => path.StartsWith(phiPath, StringComparison.OrdinalIgnoreCase));
    }

    private static RequestInfo CaptureRequestInfo(HttpContext context, string correlationId)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value 
                       ?? context.User.FindFirst(ClaimTypes.Name)?.Value;
        var ipAddress = GetClientIpAddress(context);
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();

        return new RequestInfo
        {
            CorrelationId = correlationId,
            UserId = Guid.TryParse(userId, out var uid) ? uid : null,
            UserEmail = userEmail,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            HttpMethod = context.Request.Method,
            Path = context.Request.Path.Value ?? string.Empty,
            QueryString = context.Request.QueryString.Value
        };
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded headers (load balancer/proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private async Task LogRequestAsync(
        HttpContext context,
        IAuditLogService auditLogService,
        RequestInfo requestInfo,
        long elapsedMs)
    {
        var statusCode = context.Response.StatusCode;
        var path = requestInfo.Path;
        var isPhiAccess = IsPhiPath(path);

        // Determine the action type based on the request
        var action = DetermineAction(requestInfo.HttpMethod, path, isPhiAccess);
        var outcome = DetermineOutcome(statusCode);

        // Extract resource info from path if applicable
        var (resourceType, resourceId) = ExtractResourceInfo(path);

        var entry = new AuditLogEntry
        {
            PartitionKey = requestInfo.UserId?.ToString() ?? "anonymous",
            Action = action,
            Outcome = outcome,
            UserId = requestInfo.UserId,
            UserEmail = requestInfo.UserEmail,
            IpAddress = requestInfo.IpAddress,
            UserAgent = requestInfo.UserAgent,
            HttpMethod = requestInfo.HttpMethod,
            RequestPath = path,
            StatusCode = statusCode,
            CorrelationId = requestInfo.CorrelationId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Details = $"Completed in {elapsedMs}ms"
        };

        try
        {
            await auditLogService.LogAsync(entry);
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the request
            _logger.LogError(ex, "Failed to write audit log for request {Path}", path);
        }
    }

    private static string DetermineAction(string method, string path, bool isPhiAccess)
    {
        // Specific action mapping based on path patterns
        if (path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase))
        {
            return path.ToLowerInvariant() switch
            {
                var p when p.Contains("/login") => AuditActions.LoginSuccess,
                var p when p.Contains("/logout") => AuditActions.Logout,
                var p when p.Contains("/mfa/verify") => AuditActions.MfaVerified,
                var p when p.Contains("/mfa/setup") => AuditActions.MfaSetup,
                var p when p.Contains("/password/reset") => AuditActions.PasswordReset,
                var p when p.Contains("/invitation") => AuditActions.InvitationAccepted,
                _ => "API_REQUEST"
            };
        }

        if (path.StartsWith("/api/users", StringComparison.OrdinalIgnoreCase))
        {
            return method.ToUpperInvariant() switch
            {
                "POST" when path.Contains("/invite") => AuditActions.UserInvited,
                "POST" when path.Contains("/deactivate") => AuditActions.UserDeactivated,
                "POST" when path.Contains("/reactivate") => AuditActions.UserReactivated,
                "POST" when path.Contains("/roles") => AuditActions.RoleAssigned,
                "DELETE" when path.Contains("/roles") => AuditActions.RoleRemoved,
                "DELETE" => AuditActions.UserDeleted,
                "PUT" => AuditActions.UserUpdated,
                _ => "USER_MANAGEMENT"
            };
        }

        if (isPhiAccess)
        {
            return method.ToUpperInvariant() switch
            {
                "GET" => AuditActions.PhiAccessed,
                "POST" or "PUT" or "PATCH" => AuditActions.PhiModified,
                "DELETE" => AuditActions.PhiModified,
                _ => AuditActions.PhiAccessed
            };
        }

        // Generic action based on HTTP method
        return method.ToUpperInvariant() switch
        {
            "GET" => "API_READ",
            "POST" => "API_CREATE",
            "PUT" or "PATCH" => "API_UPDATE",
            "DELETE" => "API_DELETE",
            _ => "API_REQUEST"
        };
    }

    private static string DetermineOutcome(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => AuditOutcome.Success,
            401 or 403 => AuditOutcome.Denied,
            _ => AuditOutcome.Failure
        };
    }

    private static (string? ResourceType, string? ResourceId) ExtractResourceInfo(string path)
    {
        // Parse paths like /api/users/{id} or /api/clients/{id}
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (segments.Length >= 2 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            var resourceType = segments[1];
            
            // Check if there's a GUID in the path
            if (segments.Length >= 3 && Guid.TryParse(segments[2], out _))
            {
                return (resourceType, segments[2]);
            }

            return (resourceType, null);
        }

        return (null, null);
    }

    private sealed class RequestInfo
    {
        public required string CorrelationId { get; init; }
        public Guid? UserId { get; init; }
        public string? UserEmail { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public required string HttpMethod { get; init; }
        public required string Path { get; init; }
        public string? QueryString { get; init; }
    }
}

/// <summary>
/// Extension methods for adding audit logging middleware.
/// </summary>
public static class AuditLoggingMiddlewareExtensions
{
    /// <summary>
    /// Adds the audit logging middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditLoggingMiddleware>();
    }
}
