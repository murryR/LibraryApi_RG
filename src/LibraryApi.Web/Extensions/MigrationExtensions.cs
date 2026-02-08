using Microsoft.EntityFrameworkCore;
using Serilog;
using LibraryApi.Infrastructure.Persistence;

namespace LibraryApi.Web.Extensions;

/// <summary>
/// Applies database migrations and seeds initial data at startup.
/// </summary>
public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var configuration = app.Configuration;

        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            Log.Information("Connection string: {ConnectionString}",
                connectionString?.Replace("Data Source=", "Data Source=***") ?? "Not configured");

            if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("/home/site/data"))
            {
                var dataPath = "/home/site/data";
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                    Log.Information("Created directory: {DataPath}", dataPath);
                }
                else
                {
                    Log.Information("Directory already exists: {DataPath}", dataPath);
                }
            }

            var allMigrations = context.Database.GetMigrations().ToList();
            var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
            var pendingMigrations = context.Database.GetPendingMigrations().ToList();

            Log.Information("=== Database Migration Process Started ===");
            Log.Information("Total migrations defined: {Count}, Applied: {Applied}, Pending: {Pending}",
                allMigrations.Count, appliedMigrations.Count, pendingMigrations.Count);

            if (pendingMigrations.Any())
            {
                Log.Information("Applying {Count} pending migration(s)...", pendingMigrations.Count);
                context.Database.Migrate();
                var appliedAfter = context.Database.GetAppliedMigrations().ToList();
                var newlyApplied = appliedAfter.Count - appliedMigrations.Count;
                if (newlyApplied > 0)
                    Log.Information("Successfully applied {NewlyApplied} migration(s)", newlyApplied);
            }
            else
            {
                Log.Information("No pending migrations - database is already up to date");
            }

            Log.Information("=== Database Migration Process Completed ===");

            var userSeeder = services.GetRequiredService<UserSeeder>();
            await userSeeder.SeedUsersAsync();
            Log.Information("User seeding done");

            var bookSeeder = services.GetRequiredService<BookSeeder>();
            await bookSeeder.SeedBooksAsync();
            Log.Information("Book seeding done");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database migration failed; application will not start.");
            throw;
        }
    }
}
