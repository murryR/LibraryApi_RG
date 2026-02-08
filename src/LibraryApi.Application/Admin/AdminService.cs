using Microsoft.Extensions.Logging;
using LibraryApi.Application.Catalog.Dtos;
using LibraryApi.Application.Common.Interfaces;

namespace LibraryApi.Application.Admin;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly ILogger<AdminService> _logger;

    public AdminService(IUserRepository userRepository, ILoanRepository loanRepository, ILogger<AdminService> logger)
    {
        _userRepository = userRepository;
        _loanRepository = loanRepository;
        _logger = logger;
    }

    public async Task<List<AdminUserStatsDto>> GetAllUsersWithStatsAsync(string? nameFilter = null, string? sortBy = null, string? sortDirection = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _userRepository.GetUsersForAdminAsync(nameFilter, cancellationToken);
            var loanStats = await _loanRepository.GetAllUserLoanStatsAsync(cancellationToken);
            var statsByUser = loanStats.ToDictionary(x => x.UserId, x => (x.BorrowedCount, x.ReturnedCount));

            var result = users.Select(u =>
            {
                var (borrowed, returned) = statsByUser.GetValueOrDefault(u.Id, (0, 0));
                return new AdminUserStatsDto
                {
                    UserId = u.Id,
                    UserName = u.Login,
                    BorrowedCount = borrowed,
                    ReturnedCount = returned
                };
            }).ToList();

            var orderBy = (string.IsNullOrWhiteSpace(sortBy) ? "UserName" : sortBy).Trim();
            var isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            result = orderBy switch
            {
                "BorrowedCount" => isDesc ? result.OrderByDescending(x => x.BorrowedCount).ThenBy(x => x.UserName).ToList() : result.OrderBy(x => x.BorrowedCount).ThenBy(x => x.UserName).ToList(),
                _ => isDesc ? result.OrderByDescending(x => x.UserName).ToList() : result.OrderBy(x => x.UserName).ToList()
            };

            _logger.LogInformation("Retrieved admin user stats for {Count} users", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users with stats");
            throw;
        }
    }
}
