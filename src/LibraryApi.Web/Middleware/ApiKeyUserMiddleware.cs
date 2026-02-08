using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace LibraryApi.Web.Middleware;

/// <summary>
/// Ensures that for catalog API requests, HttpContext.User is set from the ApiKey scheme only.
/// Prevents the cookie principal (e.g. admin from same browser tab) from overriding the API key principal
/// when both are sent, which would be a security risk (borrow/return attributed to wrong user).
/// </summary>
public class ApiKeyUserMiddleware
{
    private const string ApiKeyScheme = "ApiKey";
    private const string CatalogApiPathPrefix = "/api/catalog-api";

    private readonly RequestDelegate _next;

    public ApiKeyUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(CatalogApiPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var result = await context.AuthenticateAsync(ApiKeyScheme);
            if (result.Succeeded && result.Principal != null)
            {
                context.User = result.Principal;
            }
        }

        await _next(context);
    }
}
