namespace LibraryApi.Application.Common.Interfaces;

public interface IStatusRepository
{
    Task<Domain.Entities.Status?> GetFirstOrderedByIdAsync(CancellationToken cancellationToken = default);
}
