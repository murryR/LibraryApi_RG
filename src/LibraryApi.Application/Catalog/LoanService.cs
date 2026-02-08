using Microsoft.Extensions.Logging;
using LibraryApi.Application.Catalog.Dtos;
using LibraryApi.Application.Common.Dtos;
using LibraryApi.Application.Common.Interfaces;
using LibraryApi.Domain.Entities;

namespace LibraryApi.Application.Catalog;

public class LoanService : ILoanService
{
    private readonly IBookCatalogRepository _catalogRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoanService> _logger;

    public LoanService(
        IBookCatalogRepository catalogRepository,
        ILoanRepository loanRepository,
        IUnitOfWork unitOfWork,
        ILogger<LoanService> logger)
    {
        _catalogRepository = catalogRepository;
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BorrowResult> BorrowBookAsync(string bookId, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _catalogRepository.GetByIdAsync(bookId, cancellationToken);
            if (book == null)
            {
                _logger.LogWarning("Borrow failed: book not found. BookId={BookId}, UserId={UserId}", bookId, userId);
                throw new KeyNotFoundException($"Book with ID '{bookId}' not found.");
            }

            var activeLoanCount = await _loanRepository.GetActiveLoansCountByBookIdAsync(bookId, cancellationToken);
            var availableCount = book.NumberOfPieces - activeLoanCount;

            if (availableCount <= 0)
            {
                _logger.LogWarning("Borrow failed: no copies available. BookId={BookId}, UserId={UserId}",
                    bookId, userId);
                throw new InvalidOperationException($"No available copies of this book. Currently {activeLoanCount} out of {book.NumberOfPieces} are borrowed.");
            }

            var existingUserLoan = await _loanRepository.GetOldestActiveLoanAsync(bookId, userId, cancellationToken);
            if (existingUserLoan != null)
            {
                _logger.LogWarning("Borrow failed: user already has this book. BookId={BookId}, UserId={UserId}",
                    bookId, userId);
                throw new InvalidOperationException("You already have this book borrowed.");
            }

            var loan = new BookLoan
            {
                BookId = bookId,
                UserId = userId,
                BorrowedDate = DateTime.UtcNow,
                ReturnedDate = null
            };

            _loanRepository.Add(loan);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} borrowed book {BookId}, LoanId={LoanId}", userId, bookId, loan.Id);

            return new BorrowResult
            {
                LoanId = loan.Id,
                BookId = loan.BookId,
                UserId = loan.UserId,
                BorrowedDate = loan.BorrowedDate,
                Message = "Book borrowed successfully"
            };
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error borrowing book: BookId={BookId}, UserId={UserId}", bookId, userId);
            throw;
        }
    }

    public async Task ReturnBookAsync(string bookId, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var loan = await _loanRepository.GetOldestActiveLoanAsync(bookId, userId, cancellationToken);
            if (loan == null)
            {
                _logger.LogWarning("Return failed: no active loan. BookId={BookId}, UserId={UserId}", bookId, userId);
                throw new InvalidOperationException("You don't have an active loan for this book.");
            }

            var bookExists = await _catalogRepository.ExistsAsync(bookId, cancellationToken);
            if (!bookExists)
            {
                _logger.LogError("Book not found for return: BookId={BookId}, UserId={UserId}", bookId, userId);
                throw new KeyNotFoundException($"Book with ID '{bookId}' not found.");
            }

            loan.ReturnedDate = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} returned book {BookId}, LoanId={LoanId}", userId, bookId, loan.Id);
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning book: BookId={BookId}, UserId={UserId}", bookId, userId);
            throw;
        }
    }

    public async Task<BorrowStatusDto> GetBorrowStatusAsync(string bookId, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _catalogRepository.GetByIdAsync(bookId, cancellationToken);
            if (book == null)
                throw new KeyNotFoundException($"Book with ID '{bookId}' not found.");

            var counts = await _loanRepository.GetActiveLoansCountByBookIdsAsync(new[] { bookId }, userId, cancellationToken);
            var (totalCount, userCount) = counts.GetValueOrDefault(bookId, (0, 0));
            var availableCount = Math.Max(0, book.NumberOfPieces - totalCount);

            return new BorrowStatusDto
            {
                BookId = bookId,
                IsBorrowedByUser = userCount > 0,
                ActiveLoanCount = userCount,
                AvailableCount = availableCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting borrow status: BookId={BookId}, UserId={UserId}", bookId, userId);
            throw;
        }
    }

    public async Task<Dictionary<string, BorrowStatusDto>> GetBorrowStatusBatchAsync(List<string> bookIds, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (bookIds == null || bookIds.Count == 0)
                return new Dictionary<string, BorrowStatusDto>();

            var books = await _catalogRepository.GetByIdsAsync(bookIds, cancellationToken);
            var loanCounts = await _loanRepository.GetActiveLoansCountByBookIdsAsync(bookIds, userId, cancellationToken);

            var result = new Dictionary<string, BorrowStatusDto>();
            foreach (var book in books)
            {
                var (totalCount, userCount) = loanCounts.GetValueOrDefault(book.Id, (0, 0));
                result[book.Id] = new BorrowStatusDto
                {
                    BookId = book.Id,
                    IsBorrowedByUser = userCount > 0,
                    ActiveLoanCount = userCount,
                    AvailableCount = Math.Max(0, book.NumberOfPieces - totalCount)
                };
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch borrow status: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetAvailableCountAsync(string bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _catalogRepository.GetByIdAsync(bookId, cancellationToken);
            if (book == null)
                throw new KeyNotFoundException($"Book with ID '{bookId}' not found.");

            var activeLoanCount = await _loanRepository.GetActiveLoansCountByBookIdAsync(bookId, cancellationToken);
            return Math.Max(0, book.NumberOfPieces - activeLoanCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available count: BookId={BookId}", bookId);
            throw;
        }
    }

    public async Task<List<BorrowedItemDto>> GetUserBorrowedBooksAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var borrowed = await _loanRepository.GetUserBorrowedBooksWithBooksAsync(userId, cancellationToken);
            var dtos = borrowed.Select(x => new BorrowedItemDto
            {
                LoanId = x.Loan.Id,
                BookId = x.Loan.BookId,
                Name = x.Book.Name,
                Author = x.Book.Author,
                IssueYear = x.Book.IssueYear,
                ISBN = x.Book.ISBN,
                BorrowedDate = x.Loan.BorrowedDate
            }).ToList();

            _logger.LogInformation("Retrieved {Count} borrowed books for user {UserId}", dtos.Count, userId);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user borrowed books: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<List<ReturnedItemDto>> GetUserReturnedBooksAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var returned = await _loanRepository.GetUserReturnedBooksWithBooksAsync(userId, cancellationToken);
            var dtos = returned.Select(x => new ReturnedItemDto
            {
                LoanId = x.Loan.Id,
                BookId = x.Loan.BookId,
                Name = x.Book.Name,
                Author = x.Book.Author,
                IssueYear = x.Book.IssueYear,
                ISBN = x.Book.ISBN,
                ReturnedDate = x.Loan.ReturnedDate!.Value
            }).ToList();

            _logger.LogInformation("Retrieved {Count} returned books for user {UserId}", dtos.Count, userId);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user returned books: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<PagedResult<LoanHistoryItemDto>> GetUserLoanHistoryAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var pageNum = Math.Max(1, pageNumber);
            var size = Math.Max(1, Math.Min(100, pageSize));

            var (items, totalCount) = await _loanRepository.GetUserLoansWithBooksPagedAsync(userId, includeReturned: true, pageNum, size, cancellationToken);

            var dtos = items.Select(x => new LoanHistoryItemDto
            {
                LoanId = x.Loan.Id,
                BookId = x.Loan.BookId,
                Name = x.Book.Name,
                Author = x.Book.Author,
                IssueYear = x.Book.IssueYear,
                ISBN = x.Book.ISBN,
                BorrowedDate = x.Loan.BorrowedDate,
                ReturnedDate = x.Loan.ReturnedDate
            }).ToList();

            return new PagedResult<LoanHistoryItemDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNum,
                PageSize = size
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loan history: UserId={UserId}", userId);
            throw;
        }
    }
}
