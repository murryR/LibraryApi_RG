using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LibraryApi.Application.Common.Interfaces;
using LibraryApi.Infrastructure.Persistence;
using LibraryApi.Infrastructure.Persistence.Repositories;

namespace LibraryApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString, sqliteOptions =>
                sqliteOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IBookCatalogRepository, BookCatalogRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStatusRepository, StatusRepository>();

        // Register Seeders
        services.AddScoped<UserSeeder>();
        services.AddScoped<BookSeeder>();

        return services;
    }
}



