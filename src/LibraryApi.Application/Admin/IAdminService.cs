using LibraryApi.Application.Catalog.Dtos;

namespace LibraryApi.Application.Admin;

public interface IAdminService
{
    Task<List<AdminUserStatsDto>> GetAllUsersWithStatsAsync(string? nameFilter = null, string? sortBy = null, string? sortDirection = null, CancellationToken cancellationToken = default);
}
