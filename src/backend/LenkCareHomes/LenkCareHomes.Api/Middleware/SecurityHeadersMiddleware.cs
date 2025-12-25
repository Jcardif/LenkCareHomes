namespace LenkCareHomes.Api.Middleware;

/// <summary>
/// Middleware that adds security headers to all HTTP responses.
/// Implements OWASP security best practices for HIPAA compliance.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IWebHostEnvironment environment,
        ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before the response is sent
        context.Response.OnStarting(() =>
        {
            AddSecurityHeaders(context);
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent clickjacking attacks
        // X-Frame-Options: Prevents the page from being embedded in iframes
        if (!headers.ContainsKey("X-Frame-Options"))
        {
            headers["X-Frame-Options"] = "DENY";
        }

        // Prevent MIME type sniffing
        // X-Content-Type-Options: Prevents browsers from MIME-sniffing responses
        if (!headers.ContainsKey("X-Content-Type-Options"))
        {
            headers["X-Content-Type-Options"] = "nosniff";
        }

        // Enable XSS protection (for older browsers)
        // X-XSS-Protection: Enables the browser's built-in XSS filter
        if (!headers.ContainsKey("X-XSS-Protection"))
        {
            headers["X-XSS-Protection"] = "1; mode=block";
        }

        // Referrer Policy: Controls how much referrer information is sent
        if (!headers.ContainsKey("Referrer-Policy"))
        {
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }

        // Permissions Policy: Restricts browser features
        if (!headers.ContainsKey("Permissions-Policy"))
        {
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=()";
        }

        // Content Security Policy: Prevents XSS, injection attacks
        // More restrictive in production, allows inline scripts in development for hot reload
        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            var csp = _environment.IsDevelopment()
                ? "default-src 'self'; " +
                  "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                  "style-src 'self' 'unsafe-inline'; " +
                  "img-src 'self' data: blob: https:; " +
                  "font-src 'self' data:; " +
                  "connect-src 'self' ws: wss: https:; " +
                  "frame-ancestors 'none'; " +
                  "form-action 'self'; " +
                  "base-uri 'self';"
                : "default-src 'self'; " +
                  "script-src 'self'; " +
                  "style-src 'self' 'unsafe-inline'; " +
                  "img-src 'self' data: blob: https:; " +
                  "font-src 'self' data:; " +
                  "connect-src 'self' https:; " +
                  "frame-ancestors 'none'; " +
                  "form-action 'self'; " +
                  "base-uri 'self'; " +
                  "upgrade-insecure-requests;";

            headers["Content-Security-Policy"] = csp;
        }

        // Strict Transport Security: Forces HTTPS (production only)
        // HSTS: Tells browsers to only use HTTPS
        if (!_environment.IsDevelopment() && !headers.ContainsKey("Strict-Transport-Security"))
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // Cache Control: Prevents caching of sensitive data
        // Important for PHI - ensure responses are not cached
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var sensitiveEndpoints = new[]
        {
            "/api/clients", "/api/documents", "/api/incidents",
            "/api/audit", "/api/users", "/api/caregivers"
        };

        if (sensitiveEndpoints.Any(e => path.StartsWith(e)))
        {
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate, proxy-revalidate";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";
        }
    }
}

/// <summary>
/// Extension methods for adding security headers middleware.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds the security headers middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
