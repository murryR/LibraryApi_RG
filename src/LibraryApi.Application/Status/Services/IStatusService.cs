using StatusEntity = LibraryApi.Domain.Entities.Status;

namespace LibraryApi.Application.Status.Services;

public interface IStatusService
{
    Task<StatusEntity> GetFirstStatusAsync(CancellationToken cancellationToken = default);
}


