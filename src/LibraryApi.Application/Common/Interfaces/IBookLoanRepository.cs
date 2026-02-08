using LibraryApi.Domain.Entities;

namespace LibraryApi.Application.Common.Interfaces;

public interface ILoanRepository
{
    Task<int> GetActiveLoansCountByBookIdAsync(string bookId, CancellationToken cancellationToken = default);
    Task<BookLoan?> GetOldestActiveLoanAsync(string bookId, int userId, CancellationToken cancellationToken = default);
    void Add(BookLoan loan);
    Task<Dictionary<string, (int TotalCount, int UserCount)>> GetActiveLoansCountByBookIdsAsync(IReadOnlyList<string> bookIds, int userId, CancellationToken cancellationToken = default);
    Task<List<(BookLoan Loan, Book Book)>> GetUserBorrowedBooksWithBooksAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<(BookLoan Loan, Book Book)>> GetUserReturnedBooksWithBooksAsync(int userId, CancellationToken cancellationToken = default);
    Task<(List<(BookLoan Loan, Book Book)> Items, int TotalCount)> GetUserLoansWithBooksPagedAsync(int userId, bool includeReturned, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<List<(int UserId, int BorrowedCount, int ReturnedCount)>> GetAllUserLoanStatsAsync(CancellationToken cancellationToken = default);
}

