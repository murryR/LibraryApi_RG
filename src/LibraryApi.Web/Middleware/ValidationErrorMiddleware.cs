using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LibraryApi.Web.Middleware;

public class ValidationErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationErrorMiddleware> _logger;

    public ValidationErrorMiddleware(RequestDelegate next, ILogger<ValidationErrorMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api") &&
            context.Request.Method == "POST" &&
            context.Request.ContentType?.Contains("application/json") == true)
        {
            var endpoint = context.GetEndpoint();
            var endpointName = endpoint?.DisplayName ?? "No endpoint matched";
            _logger.LogInformation("ValidationErrorMiddleware - Endpoint: {Endpoint}, Route: {Route}", endpointName, context.Request.Path);

            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            _logger.LogInformation("ValidationErrorMiddleware - After next middleware. StatusCode: {StatusCode}, ResponseBodyLength: {Length}, HasStarted: {HasStarted}, Endpoint: {Endpoint}",
                context.Response.StatusCode, responseBody.Length, context.Response.HasStarted, endpointName);

            if (context.Response.StatusCode == 400 && responseBody.Length == 0)
            {
                _logger.LogWarning("ValidationErrorMiddleware - 400 response with no body detected. Writing error response.");

                var errorResponse = new
                {
                    status = 400,
                    title = "Bad Request",
                    detail = "Request validation failed. Please check your input.",
                    errors = new Dictionary<string, string[]>
                    {
                        ["general"] = new[] { "Unable to bind request. Please verify the JSON format and property names." }
                    }
                };

                responseBody.SetLength(0);
                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(responseBody, errorResponse);
                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBodyStream);
            }
            else
            {
                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBodyStream);
            }

            context.Response.Body = originalBodyStream;
        }
        else
        {
            await _next(context);
        }
    }
}
