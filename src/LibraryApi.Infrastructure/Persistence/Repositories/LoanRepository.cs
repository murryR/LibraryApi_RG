using Microsoft.EntityFrameworkCore;
using LibraryApi.Application.Common.Interfaces;
using LibraryApi.Domain.Entities;
using LibraryApi.Infrastructure.Persistence;

namespace LibraryApi.Infrastructure.Persistence.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly AppDbContext _context;

    public LoanRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetActiveLoansCountByBookIdAsync(string bookId, CancellationToken cancellationToken = default)
    {
        return await _context.BookLoans
            .CountAsync(bl => bl.BookId == bookId && bl.ReturnedDate == null, cancellationToken);
    }

    public async Task<BookLoan?> GetOldestActiveLoanAsync(string bookId, int userId, CancellationToken cancellationToken = default)
    {
        return await _context.BookLoans
            .Where(bl => bl.BookId == bookId && bl.UserId == userId && bl.ReturnedDate == null)
            .OrderBy(bl => bl.BorrowedDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(BookLoan loan)
    {
        _context.BookLoans.Add(loan);
    }

    public async Task<Dictionary<string, (int TotalCount, int UserCount)>> GetActiveLoansCountByBookIdsAsync(IReadOnlyList<string> bookIds, int userId, CancellationToken cancellationToken = default)
    {
        if (bookIds == null || bookIds.Count == 0)
            return new Dictionary<string, (int TotalCount, int UserCount)>();

        var activeLoans = await _context.BookLoans
            .Where(bl => bookIds.Contains(bl.BookId) && bl.ReturnedDate == null)
            .GroupBy(bl => bl.BookId)
            .Select(g => new
            {
                BookId = g.Key,
                TotalCount = g.Count(),
                UserCount = g.Count(bl => bl.UserId == userId)
            })
            .ToListAsync(cancellationToken);

        return activeLoans.ToDictionary(x => x.BookId, x => (x.TotalCount, x.UserCount));
    }

    public async Task<List<(BookLoan Loan, Book Book)>> GetUserBorrowedBooksWithBooksAsync(int userId, CancellationToken cancellationToken = default)
    {
        var joined = await _context.BookLoans
            .Where(bl => bl.UserId == userId && bl.ReturnedDate == null)
            .Join(
                _context.Books,
                loan => loan.BookId,
                book => book.Id,
                (loan, book) => new { Loan = loan, Book = book })
            .OrderBy(x => x.Loan.BorrowedDate)
            .ToListAsync(cancellationToken);

        return joined.Select(x => (x.Loan, x.Book)).ToList();
    }

    public async Task<List<(BookLoan Loan, Book Book)>> GetUserReturnedBooksWithBooksAsync(int userId, CancellationToken cancellationToken = default)
    {
        var joined = await _context.BookLoans
            .Where(bl => bl.UserId == userId && bl.ReturnedDate != null)
            .Join(
                _context.Books,
                loan => loan.BookId,
                book => book.Id,
                (loan, book) => new { Loan = loan, Book = book })
            .OrderByDescending(x => x.Loan.ReturnedDate)
            .ToListAsync(cancellationToken);

        return joined.Select(x => (x.Loan, x.Book)).ToList();
    }

    public async Task<(List<(BookLoan Loan, Book Book)> Items, int TotalCount)> GetUserLoansWithBooksPagedAsync(int userId, bool includeReturned, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.BookLoans
            .Where(bl => bl.UserId == userId);
        if (!includeReturned)
            query = query.Where(bl => bl.ReturnedDate == null);

        var baseQuery = query
            .Join(_context.Books, loan => loan.BookId, book => book.Id, (loan, book) => new { Loan = loan, Book = book })
            .OrderByDescending(x => x.Loan.BorrowedDate);

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var pageNum = Math.Max(1, pageNumber);
        var size = Math.Max(1, Math.Min(100, pageSize));
        var skip = (pageNum - 1) * size;

        var joined = await baseQuery
            .Skip(skip)
            .Take(size)
            .ToListAsync(cancellationToken);

        var items = joined.Select(x => (x.Loan, x.Book)).ToList();
        return (items, totalCount);
    }

    public async Task<List<(int UserId, int BorrowedCount, int ReturnedCount)>> GetAllUserLoanStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = await _context.BookLoans
            .GroupBy(bl => bl.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                BorrowedCount = g.Count(bl => bl.ReturnedDate == null),
                ReturnedCount = g.Count(bl => bl.ReturnedDate != null)
            })
            .ToListAsync(cancellationToken);

        return stats.Select(x => (x.UserId, x.BorrowedCount, x.ReturnedCount)).ToList();
    }
}

