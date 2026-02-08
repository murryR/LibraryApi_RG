using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Serilog;

namespace LibraryApi.Web.Extensions;

/// <summary>
/// Configures Cookie and API Key authentication for LibraryApi.
/// </summary>
public static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddLibraryApiAuthentication(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var isProduction = builder.Environment.IsProduction();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/";
                options.LogoutPath = "/Logout";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Events.OnSigningIn = context => { context.Properties.IsPersistent = true; return Task.CompletedTask; };
                options.Events.OnValidatePrincipal = context =>
                {
                    var cookieName = options.Cookie.Name ?? ".AspNetCore.Cookies";
                    var hasCookie = context.Request.Cookies.ContainsKey(cookieName);
                    var isAuth = context.Principal?.Identity?.IsAuthenticated ?? false;
                    Log.Information("OnValidatePrincipal - Path: {Path}, Method: {Method}, HasCookie: {HasCookie}, IsAuthenticated: {IsAuthenticated}",
                        context.HttpContext.Request.Path, context.HttpContext.Request.Method, hasCookie, isAuth);
                    if (hasCookie && !isAuth)
                    {
                        Log.Warning("Cookie validation failed. Path: {Path}", context.HttpContext.Request.Path);
                        context.RejectPrincipal();
                    }
                    return Task.CompletedTask;
                };
                options.Events.OnSignedIn = context =>
                {
                    Log.Information("User {UserName} signed in successfully", context.Principal?.Identity?.Name ?? "Unknown");
                    return Task.CompletedTask;
                };
                options.Events.OnSigningOut = context =>
                {
                    Log.Information("User {UserName} is signing out", context.HttpContext.User?.Identity?.Name ?? "Unknown");
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToLogin = context =>
                {
                    Log.Warning("OnRedirectToLogin - Path: {Path}, Method: {Method}", context.HttpContext.Request.Path, context.HttpContext.Request.Method);
                    context.RedirectUri = "/";
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    Log.Warning("OnRedirectToAccessDenied - Path: {Path}, UserName: {UserName}", context.HttpContext.Request.Path, context.HttpContext.User?.Identity?.Name ?? "Unknown");
                    context.RedirectUri = "/";
                    return Task.CompletedTask;
                };
            })
            .AddScheme<AuthenticationSchemeOptions, Authentication.ApiKeyAuthenticationHandler>("ApiKey", _ => { });

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
        });

        return builder;
    }
}
