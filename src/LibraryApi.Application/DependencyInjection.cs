using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using LibraryApi.Application.Admin;
using LibraryApi.Application.Auth;
using LibraryApi.Application.Catalog;
using LibraryApi.Application.Catalog.Services;
using LibraryApi.Application.Status.Services;

namespace LibraryApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IStatusService, StatusService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<IIsbnValidationService, IsbnValidationService>();
        services.AddScoped<IAdminService, AdminService>();

        // Register FluentValidation validators from Application assembly
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}


