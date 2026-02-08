using Serilog;
using Serilog.Events;

namespace LibraryApi.Web.Extensions;

/// <summary>
/// Configures structured logging for LibraryApi (console + file, environment-aware paths).
/// </summary>
public static class LoggingExtensions
{
    public static void AddLibraryApiLogging(this WebApplicationBuilder builder)
    {
        var loggingPath = Directory.Exists("/home/site/data")
            ? "/home/site/data/logs/log-.txt"
            : "logs/log-.txt";
        var loggingDir = Path.GetDirectoryName(loggingPath) ?? "logs";
        if (!Directory.Exists(loggingDir))
            Directory.CreateDirectory(loggingDir);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: loggingPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .CreateLogger();

        builder.Host.UseSerilog();
        Log.Information("Logging configured; directory: {Dir}", loggingDir);
    }
}
