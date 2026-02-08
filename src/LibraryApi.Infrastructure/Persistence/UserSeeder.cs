using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LibraryApi.Domain.Entities;
using LibraryApi.Domain.Enums;

namespace LibraryApi.Infrastructure.Persistence;

public class UserSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserSeeder> _logger;

    public UserSeeder(AppDbContext context, ILogger<UserSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if users already exist
            if (await _context.ApplicationUsers.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Users already exist in database. Skipping seed.");
                return;
            }

            var users = new List<ApplicationUser>
            {
                new ApplicationUser
                {
                    Id = 1,
                    Login = "simpleUser",
                    Password = "userPass",
                    UserType = UserType.User,
                    ApiKey = string.Empty,
                    Permissions = Permissions.Standard
                },
               new ApplicationUser
                {
                    Id = 2,
                    Login = "adminUser",
                    Password = "adminPass",
                    UserType = UserType.Admin,
                    ApiKey = string.Empty,
                    Permissions = Permissions.Elevated
                },
                new ApplicationUser
                {
                    Id = 3,
                    Login = "apiUser",
                    Password = "appiPass",
                    UserType = UserType.API,
                    ApiKey = "apiUserPass1234", 
                    Permissions = Permissions.Standard
                }
            };

            await _context.ApplicationUsers.AddRangeAsync(users, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Seeded {users.Count} users successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding users");
            throw;
        }
    }
}


