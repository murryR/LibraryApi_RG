using Microsoft.AspNetCore.DataProtection;
using Serilog;

namespace LibraryApi.Web.Extensions;

/// <summary>
/// Configures data protection (cookie encryption keys) with environment-aware storage.
/// </summary>
public static class DataProtectionExtensions
{
    public static void AddLibraryApiDataProtection(this WebApplicationBuilder builder)
    {
        var keysDir = Directory.Exists("/home/site/data") ? "/home/site/data/keys" : "keys";
        if (!Directory.Exists(keysDir))
        {
            Directory.CreateDirectory(keysDir);
            Log.Information("Data Protection keys directory created: {Dir}", keysDir);
        }

        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysDir))
            .SetApplicationName("LibraryApi")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
    }
}
