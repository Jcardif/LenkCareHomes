using System.Collections.Concurrent;

namespace LenkCareHomes.Api.Middleware;

/// <summary>
///     Middleware that implements rate limiting for authentication endpoints.
///     Protects against brute force attacks while being lenient in development.
/// </summary>
public sealed class RateLimitingMiddleware
{
    // Rate limit settings
    private const int DevRequestsPerMinute = 100;
    private const int ProdRequestsPerMinute = 20;
    private const int DevBurstLimit = 30;
    private const int ProdBurstLimit = 10;

    // In-memory store for rate limiting (in production, use Redis or similar)
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _rateLimits = new();

    // Paths that are rate limited
    private static readonly HashSet<string> RateLimitedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/mfa/verify",
        "/api/auth/mfa/verify-backup",
        "/api/auth/password/reset-request",
        "/api/auth/password/reset",
        "/api/auth/invitation/accept"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RequestDelegate _next;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IWebHostEnvironment environment,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Only rate limit specific paths
        if (!ShouldRateLimit(path))
        {
            await _next(context);
            return;
        }

        var clientKey = GetClientKey(context);
        var now = DateTime.UtcNow;

        // Get or create rate limit entry
        var entry = _rateLimits.GetOrAdd(clientKey, _ => new RateLimitEntry());

        lock (entry)
        {
            // Reset counters if window has passed
            if (now - entry.WindowStart > TimeSpan.FromMinutes(1))
            {
                entry.WindowStart = now;
                entry.RequestCount = 0;
                entry.BurstCount = 0;
                entry.LastBurstReset = now;
            }

            // Reset burst counter every 10 seconds
            if (now - entry.LastBurstReset > TimeSpan.FromSeconds(10))
            {
                entry.BurstCount = 0;
                entry.LastBurstReset = now;
            }

            entry.RequestCount++;
            entry.BurstCount++;

            var maxRequests = _environment.IsDevelopment() ? DevRequestsPerMinute : ProdRequestsPerMinute;
            var maxBurst = _environment.IsDevelopment() ? DevBurstLimit : ProdBurstLimit;

            // Check rate limits
            if (entry.RequestCount > maxRequests || entry.BurstCount > maxBurst)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for {ClientKey} on {Path}. Requests: {Requests}/{Max}, Burst: {Burst}/{MaxBurst}",
                    clientKey, path, entry.RequestCount, maxRequests, entry.BurstCount, maxBurst);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = "60";
                context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = "0";
                context.Response.Headers["X-RateLimit-Reset"] = entry.WindowStart.AddMinutes(1).ToString("o");

                return;
            }

            // Add rate limit headers to successful requests
            context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = (maxRequests - entry.RequestCount).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = entry.WindowStart.AddMinutes(1).ToString("o");
        }

        await _next(context);

        // Cleanup old entries periodically
        if (DateTime.UtcNow.Second == 0) CleanupOldEntries();
    }

    private static bool ShouldRateLimit(string path)
    {
        return RateLimitedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetClientKey(HttpContext context)
    {
        // Use IP address as the client key
        // In production with load balancer, check X-Forwarded-For
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor)) return forwardedFor.Split(',').First().Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static void CleanupOldEntries()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        var keysToRemove = _rateLimits
            .Where(kvp => kvp.Value.WindowStart < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove) _rateLimits.TryRemove(key, out _);
    }

    private sealed class RateLimitEntry
    {
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        public int RequestCount { get; set; }
        public int BurstCount { get; set; }
        public DateTime LastBurstReset { get; set; } = DateTime.UtcNow;
    }
}

/// <summary>
///     Extension methods for adding rate limiting middleware.
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    /// <summary>
    ///     Adds the rate limiting middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}