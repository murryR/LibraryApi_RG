using LibraryApi.Application.Catalog.Dtos;
using LibraryApi.Application.Common.Dtos;

namespace LibraryApi.Application.Catalog;

public interface ILoanService
{
    Task<BorrowResult> BorrowBookAsync(string bookId, int userId, CancellationToken cancellationToken = default);
    Task ReturnBookAsync(string bookId, int userId, CancellationToken cancellationToken = default);
    Task<BorrowStatusDto> GetBorrowStatusAsync(string bookId, int userId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, BorrowStatusDto>> GetBorrowStatusBatchAsync(List<string> bookIds, int userId, CancellationToken cancellationToken = default);
    Task<int> GetAvailableCountAsync(string bookId, CancellationToken cancellationToken = default);
    Task<List<BorrowedItemDto>> GetUserBorrowedBooksAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<ReturnedItemDto>> GetUserReturnedBooksAsync(int userId, CancellationToken cancellationToken = default);
    Task<PagedResult<LoanHistoryItemDto>> GetUserLoanHistoryAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
