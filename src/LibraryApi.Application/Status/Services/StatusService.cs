using Microsoft.Extensions.Logging;
using LibraryApi.Application.Common.Interfaces;
using StatusEntity = LibraryApi.Domain.Entities.Status;

namespace LibraryApi.Application.Status.Services;

public class StatusService : IStatusService
{
    private readonly IStatusRepository _statusRepository;
    private readonly ILogger<StatusService> _logger;

    public StatusService(IStatusRepository statusRepository, ILogger<StatusService> logger)
    {
        _statusRepository = statusRepository;
        _logger = logger;
    }

    public async Task<StatusEntity> GetFirstStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _statusRepository.GetFirstOrderedByIdAsync(cancellationToken);

            if (status == null)
            {
                return new StatusEntity { Value = string.Empty };
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the first status");
            throw;
        }
    }
}
