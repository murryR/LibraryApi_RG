using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace LibraryApi.Web.Filters;

/// <summary>
/// Result filter to log ModelState errors after action execution
/// </summary>
public class ModelBindingLoggingFilter : IResultFilter
{
    private readonly ILogger<ModelBindingLoggingFilter> _logger;

    public ModelBindingLoggingFilter(ILogger<ModelBindingLoggingFilter> logger)
    {
        _logger = logger;
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        var actionName = context.ActionDescriptor.DisplayName ?? "Unknown";
        var route = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;

        _logger.LogInformation(
            "=== ModelBindingLoggingFilter - OnResultExecuting === Action: {Action}, Route: {Route}, ResultType: {ResultType}",
            actionName, route, context.Result?.GetType().Name ?? "null");

        if (context.ModelState != null)
        {
            _logger.LogInformation(
                "ModelBindingLoggingFilter - ModelState.IsValid: {IsValid}, ErrorCount: {ErrorCount}",
                context.ModelState.IsValid, context.ModelState.ErrorCount);

            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => new
                    {
                        Property = x.Key,
                        ErrorMessage = e.ErrorMessage,
                        Exception = e.Exception?.Message
                    }))
                    .ToList();

                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelBindingLoggingFilter - ModelState Error: Property={Property}, Error={Error}, Exception={Exception}",
                        error.Property, error.ErrorMessage, error.Exception ?? "None");
                }
            }
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
