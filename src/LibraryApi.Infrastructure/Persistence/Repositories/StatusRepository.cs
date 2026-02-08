using Microsoft.EntityFrameworkCore;
using LibraryApi.Application.Common.Interfaces;
using LibraryApi.Domain.Entities;
using LibraryApi.Infrastructure.Persistence;

namespace LibraryApi.Infrastructure.Persistence.Repositories;

public class StatusRepository : IStatusRepository
{
    private readonly AppDbContext _context;

    public StatusRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Status?> GetFirstOrderedByIdAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Statuses
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
