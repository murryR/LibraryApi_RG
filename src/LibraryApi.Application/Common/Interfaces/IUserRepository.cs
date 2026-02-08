using LibraryApi.Domain.Entities;

namespace LibraryApi.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByLoginAndPasswordAsync(string login, string password, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<List<(int Id, string Login)>> GetUsersForAdminAsync(string? nameFilter, CancellationToken cancellationToken = default);
}
