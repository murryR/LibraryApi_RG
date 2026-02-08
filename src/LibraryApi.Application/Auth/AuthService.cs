using Microsoft.Extensions.Logging;
using LibraryApi.Application.Common.Interfaces;
using LibraryApi.Domain.Entities;

namespace LibraryApi.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository userRepository, ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ApplicationUser?> ValidateLoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByLoginAndPasswordAsync(login, password, cancellationToken);

            if (user != null)
            {
                _logger.LogInformation("User {Login} authenticated successfully", login);
            }
            else
            {
                _logger.LogWarning("Failed login attempt for user {Login}", login);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while validating login for user {Login}", login);
            throw;
        }
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _userRepository.GetByIdAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving user {UserId}", userId);
            throw;
        }
    }

    public async Task<ApplicationUser?> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return null;
            }

            var user = await _userRepository.GetByApiKeyAsync(apiKey, cancellationToken);

            if (user != null)
            {
                _logger.LogInformation("API key validated successfully for user {UserId}", user.Id);
            }
            else
            {
                _logger.LogWarning("Failed API key validation attempt");
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while validating API key");
            throw;
        }
    }
}
