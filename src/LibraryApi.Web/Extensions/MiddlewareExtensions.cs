using Microsoft.AspNetCore.HttpOverrides;

namespace LibraryApi.Web.Extensions;

/// <summary>
/// Configures the HTTP request pipeline (middleware) for LibraryApi.
/// </summary>
public static class MiddlewareExtensions
{
    public static WebApplication UseLibraryApiMiddleware(this WebApplication app)
    {
        app.UseMiddleware<Middleware.ApiExceptionMiddleware>();
        app.UseExceptionHandler("/Error");
        app.UseStatusCodePages(context =>
        {
            if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
                return Task.CompletedTask;
            if (!context.HttpContext.Response.HasStarted)
            {
                context.HttpContext.Response.ContentType = "text/plain";
                var statusCode = context.HttpContext.Response.StatusCode;
                var description = statusCode switch
                {
                    400 => "Bad Request",
                    401 => "Unauthorized",
                    403 => "Forbidden",
                    404 => "Not Found",
                    500 => "Internal Server Error",
                    _ => "Error"
                };
                return context.HttpContext.Response.WriteAsync($"Status Code: {statusCode}; {description}");
            }
            return Task.CompletedTask;
        });

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "LibraryApi v1");
            c.RoutePrefix = "swagger";
        });

        if (app.Environment.IsProduction())
        {
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                RequireHeaderSymmetry = false,
                ForwardLimit = 1
            };
            forwardedHeadersOptions.KnownProxies.Clear();
            forwardedHeadersOptions.KnownNetworks.Clear();
            app.UseForwardedHeaders(forwardedHeadersOptions);
        }

        app.UseMiddleware<Middleware.HttpRequestLoggingMiddleware>();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseMiddleware<Middleware.ApiKeyUserMiddleware>();
        app.UseAuthorization();
        app.UseMiddleware<Middleware.ValidationErrorMiddleware>();
        app.MapRazorPages();
        app.MapControllers();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        return app;
    }
}
