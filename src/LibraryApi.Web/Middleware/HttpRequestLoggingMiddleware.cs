using System.Text;
using Microsoft.Extensions.Logging;

namespace LibraryApi.Web.Middleware;

/// <summary>
/// Middleware to log incoming API requests for debugging and audit
/// </summary>
public class HttpRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpRequestLoggingMiddleware> _logger;

    public HttpRequestLoggingMiddleware(RequestDelegate next, ILogger<HttpRequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Request.EnableBuffering();

            var method = context.Request.Method;
            var path = context.Request.Path + context.Request.QueryString;
            var contentType = context.Request.ContentType ?? "unknown";

            string? requestBody = null;
            if (method is "POST" or "PUT" or "PATCH")
            {
                try
                {
                    var originalBodyStream = context.Request.Body;
                    context.Request.Body.Position = 0;
                    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                    requestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read request body for logging");
                }
            }

            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
            var authType = context.User?.Identity?.AuthenticationType ?? "None";
            var userName = context.User?.Identity?.Name ?? "Anonymous";

            var cookieCount = context.Request.Cookies.Count;
            var hasAuthCookie = context.Request.Cookies.ContainsKey(".AspNetCore.Cookies");
            var cookieNames = string.Join(", ", context.Request.Cookies.Keys);

            var hasApiKeyHeader = context.Request.Headers.ContainsKey("X-API-Key");
            var hasAuthorizationHeader = context.Request.Headers.ContainsKey("Authorization");

            _logger.LogInformation(
                "API Request: {Method} {Path}, ContentType: {ContentType}, Body: {RequestBody}",
                method, path, contentType, requestBody ?? "(none)");

            _logger.LogInformation(
                "API Request Auth: IsAuthenticated: {IsAuthenticated}, AuthType: {AuthType}, UserName: {UserName}, " +
                "Cookies: {CookieCount} (HasAuthCookie: {HasAuthCookie}), CookieNames: [{CookieNames}], " +
                "HasApiKeyHeader: {HasApiKeyHeader}, HasAuthorizationHeader: {HasAuthorizationHeader}",
                isAuthenticated, authType, userName, cookieCount, hasAuthCookie, cookieNames,
                hasApiKeyHeader, hasAuthorizationHeader);
        }

        await _next(context);

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var hasContentLength = context.Response.ContentLength > 0;
            var contentType = context.Response.ContentType ?? "none";
            var hasStarted = context.Response.HasStarted;
            _logger.LogInformation(
                "API Response: {Method} {Path} - Status: {StatusCode}, ContentType: {ContentType}, ContentLength: {ContentLength}, HasStarted: {HasStarted}",
                context.Request.Method,
                context.Request.Path + context.Request.QueryString,
                context.Response.StatusCode,
                contentType,
                context.Response.ContentLength ?? 0,
                hasStarted);
        }
    }
}
