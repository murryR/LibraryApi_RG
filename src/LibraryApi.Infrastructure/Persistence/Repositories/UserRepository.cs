using Microsoft.EntityFrameworkCore;
using LibraryApi.Application.Common.Interfaces;
using LibraryApi.Domain.Entities;
using LibraryApi.Domain.Enums;
using LibraryApi.Infrastructure.Persistence;

namespace LibraryApi.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationUser?> GetByLoginAndPasswordAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Login == login && u.Password == password, cancellationToken);
    }

    public async Task<ApplicationUser?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<ApplicationUser?> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationUsers
            .FirstOrDefaultAsync(u => u.UserType == UserType.API && u.ApiKey == apiKey, cancellationToken);
    }

    public async Task<List<(int Id, string Login)>> GetUsersForAdminAsync(string? nameFilter, CancellationToken cancellationToken = default)
    {
        var query = _context.ApplicationUsers.AsQueryable();
        // .Where(u => u.UserType == UserType.User || u.UserType == UserType.Admin || u.UserType == UserType.API);

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            query = query.Where(u => EF.Functions.Like(u.Login ?? string.Empty, $"%{nameFilter.Trim()}%"));
        }

        return await query
            .Select(u => new ValueTuple<int, string>(u.Id, u.Login ?? string.Empty))
            .ToListAsync(cancellationToken);
    }
}
