using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApi.Web.Middleware;

/// <summary>
/// Maps unhandled exceptions on API routes to RFC 7807 Problem Details (JSON).
/// Non-API requests are left to the global exception handler.
/// </summary>
public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                await HandleApiExceptionAsync(context, ex);
                return;
            }
            _logger.LogError(ex, "Unhandled exception: {Path}", context.Request.Path);
            throw;
        }
    }

    private async Task HandleApiExceptionAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "API error: {Path} - {Message}", context.Request.Path, ex.Message);
        var (statusCode, title, detail) = MapExceptionToProblem(ex);
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };
        if (ex is ValidationException validationEx)
        {
            problem.Extensions["errors"] = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static (int StatusCode, string Title, string? Detail) MapExceptionToProblem(Exception ex)
    {
        return ex switch
        {
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Not Found", ex.Message),
            ValidationException => ((int)HttpStatusCode.BadRequest, "Validation Error", ex.Message),
            InvalidOperationException inv when inv.Message.Contains("No available copies", StringComparison.OrdinalIgnoreCase)
                => ((int)HttpStatusCode.Conflict, "Conflict", ex.Message),
            InvalidOperationException inv when inv.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                => ((int)HttpStatusCode.Conflict, "Conflict", ex.Message),
            InvalidOperationException => ((int)HttpStatusCode.BadRequest, "Bad Request", ex.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };
    }
}
