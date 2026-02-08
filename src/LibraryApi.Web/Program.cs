using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using LibraryApi.Application;
using LibraryApi.Infrastructure;
using LibraryApi.Web.Extensions;
using LibraryApi.Web.Swagger;

namespace LibraryApi.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddLibraryApiLogging();

        // Core services
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApplication();

        builder.Services.AddControllers(options =>
        {
            options.Filters.Add<Filters.ValidationLoggingFilter>();
            options.Filters.Add<Filters.ModelBindingLoggingFilter>();
        })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("Api.Validation");
                    if (context.HttpContext.Response.HasStarted)
                    {
                        logger.LogWarning("Validation response skipped: response already started");
                        return new Microsoft.AspNetCore.Mvc.BadRequestResult();
                    }
                    var actionName = context.ActionDescriptor.DisplayName ?? "Unknown";
                    var route = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                    var errors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => new
                        {
                            Property = x.Key,
                            ErrorMessage = e.ErrorMessage,
                            Exception = e.Exception?.Message,
                            AttemptedValue = x.Value.AttemptedValue
                        }))
                        .ToList();
                    logger.LogWarning("Validation failed for {Action} at {Route}; errors: {Count}",
                        actionName, route, errors.Count);
                    var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Status = 400,
                        Title = "Validation Error",
                        Detail = "One or more validation errors occurred.",
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    };
                    var errorDict = new Dictionary<string, string[]>();
                    foreach (var error in errors)
                    {
                        if (!errorDict.ContainsKey(error.Property))
                            errorDict[error.Property] = Array.Empty<string>();
                        var list = errorDict[error.Property].ToList();
                        list.Add(error.ErrorMessage);
                        errorDict[error.Property] = list.ToArray();
                    }
                    problemDetails.Extensions["errors"] = errorDict;
                    context.HttpContext.Response.ContentType = "application/json";
                    return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problemDetails);
                };
                options.SuppressModelStateInvalidFilter = false;
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.WriteIndented = false;
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "LibraryApi – Catalog & Loans",
                Version = "v1",
                Description = "REST API for book catalog and loan management (Zadani .NET)"
            });
            
            // Add API Key authentication support
            c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "API Key authentication using X-API-Key header",
                Name = "X-API-Key",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "ApiKeyScheme"
            });
            
            // Add operation filter to apply API Key security requirement to endpoints with ApiKey authentication
            c.OperationFilter<ApiKeyOperationFilter>();
        });

        builder.Services.AddTransient<Http.CookieForwardingHandler>();
        builder.Services.AddHttpClient("BlazorServer")
            .AddHttpMessageHandler<Http.CookieForwardingHandler>();
        
        builder.Services.AddScoped(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var navigationManager = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
            
            var httpClient = httpClientFactory.CreateClient("BlazorServer");
            httpClient.BaseAddress = new Uri(navigationManager.BaseUri);
            
            return httpClient;
        });

        builder.Services.AddHttpContextAccessor();
        builder.AddLibraryApiDataProtection();
        builder.AddLibraryApiAuthentication();

        var app = builder.Build();

        await app.ApplyMigrationsAndSeedAsync();
        app.UseLibraryApiMiddleware();

        try
        {
            Log.Information("LibraryApi started");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

