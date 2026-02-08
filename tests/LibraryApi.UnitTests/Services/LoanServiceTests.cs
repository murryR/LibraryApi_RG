using LibraryApi.Application.Catalog;
using LibraryApi.Application.Common.Interfaces;
using LibraryApi.Domain.Entities;
using LibraryApi.Infrastructure.Persistence;
using LibraryApi.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApi.UnitTests.Services;

public class LoanServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly IBookCatalogRepository _catalogRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoanService> _logger;

    public LoanServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new AppDbContext(options);
        _catalogRepository = new BookCatalogRepository(_dbContext);
        _loanRepository = new LoanRepository(_dbContext);
        _unitOfWork = _dbContext;
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<LoanService>.Instance;
    }

    private ILoanService CreateService() => new LoanService(_catalogRepository, _loanRepository, _unitOfWork, _logger);

    [Fact]
    public async Task BorrowBookAsync_AvailableCopies_BorrowsSuccessfully()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        var result = await service.BorrowBookAsync("book1", 1);

        Assert.NotNull(result);
        Assert.Equal("book1", result.BookId);
        Assert.Equal(1, result.UserId);
        Assert.NotEmpty(result.LoanId);
        Assert.Equal("Book borrowed successfully", result.Message);
    }

    [Fact]
    public async Task BorrowBookAsync_AllCopiesBorrowed_ThrowsInvalidOperationException()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 2 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 2, BorrowedDate = DateTime.UtcNow });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 3, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BorrowBookAsync("book1", 1));
    }

    [Fact]
    public async Task BorrowBookAsync_UserAlreadyHasBook_ThrowsInvalidOperationException()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.BorrowBookAsync("book1", 1));
        Assert.Equal("You already have this book borrowed.", ex.Message);
    }

    [Fact]
    public async Task BorrowBookAsync_NonExistentBook_ThrowsKeyNotFoundException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.BorrowBookAsync("nonexistent", 1));
    }

    [Fact]
    public async Task ReturnBookAsync_ActiveLoan_ReturnsSuccessfully()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        var loan = new BookLoan { Id = "loan1", BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow.AddDays(-5) };
        _dbContext.BookLoans.Add(loan);
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        await service.ReturnBookAsync("book1", 1);

        Assert.NotNull(loan.ReturnedDate);
    }

    [Fact]
    public async Task ReturnBookAsync_NoActiveLoan_ThrowsInvalidOperationException()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReturnBookAsync("book1", 1));
    }

    [Fact]
    public async Task GetBorrowStatusAsync_BookExists_ReturnsCorrectStatus()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 2, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetBorrowStatusAsync("book1", 1);

        Assert.NotNull(result);
        Assert.Equal("book1", result.BookId);
        Assert.True(result.IsBorrowedByUser);
        Assert.Equal(1, result.ActiveLoanCount);
        Assert.Equal(3, result.AvailableCount);
    }

    [Fact]
    public async Task GetBorrowStatusAsync_UserNotBorrowed_ReturnsCorrectStatus()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 2, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetBorrowStatusAsync("book1", 1);

        Assert.NotNull(result);
        Assert.False(result.IsBorrowedByUser);
        Assert.Equal(0, result.ActiveLoanCount);
        Assert.Equal(4, result.AvailableCount);
    }

    [Fact]
    public async Task GetBorrowStatusAsync_NonExistentBook_ThrowsKeyNotFoundException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetBorrowStatusAsync("nonexistent", 1));
    }

    [Fact]
    public async Task GetAvailableCountAsync_BookExists_ReturnsCorrectCount()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 2, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetAvailableCountAsync("book1");

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task GetAvailableCountAsync_NonExistentBook_ThrowsKeyNotFoundException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetAvailableCountAsync("nonexistent"));
    }

    [Fact]
    public async Task GetUserBorrowedBooksAsync_UserHasLoans_ReturnsBooks()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "book2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        _dbContext.BookLoans.Add(new BookLoan { Id = "loan1", BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow.AddDays(-5) });
        _dbContext.BookLoans.Add(new BookLoan { Id = "loan2", BookId = "book2", UserId = 1, BorrowedDate = DateTime.UtcNow.AddDays(-2) });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetUserBorrowedBooksAsync(1);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Book 1", result[0].Name);
        Assert.Equal("Book 2", result[1].Name);
    }

    [Fact]
    public async Task GetBorrowStatusBatchAsync_NullList_ReturnsEmptyDictionary()
    {
        var service = CreateService();

        var result = await service.GetBorrowStatusBatchAsync(null!, 1);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBorrowStatusBatchAsync_EmptyList_ReturnsEmptyDictionary()
    {
        var service = CreateService();

        var result = await service.GetBorrowStatusBatchAsync(new List<string>(), 1);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBorrowStatusBatchAsync_MultipleBooks_ReturnsCorrectStatus()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "book2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 2, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetBorrowStatusBatchAsync(new List<string> { "book1", "book2" }, 1);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("book1"));
        Assert.True(result.ContainsKey("book2"));
        Assert.True(result["book1"].IsBorrowedByUser);
        Assert.Equal(1, result["book1"].ActiveLoanCount);
        Assert.Equal(3, result["book1"].AvailableCount);
        Assert.False(result["book2"].IsBorrowedByUser);
        Assert.Equal(0, result["book2"].ActiveLoanCount);
        Assert.Equal(3, result["book2"].AvailableCount);
    }

    [Fact]
    public async Task GetBorrowStatusBatchAsync_MixedLoanStatus_ReturnsCorrectCounts()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 3 });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow.AddDays(-1) });
        _dbContext.BookLoans.Add(new BookLoan { BookId = "book1", UserId = 2, BorrowedDate = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetBorrowStatusBatchAsync(new List<string> { "book1" }, 1);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result["book1"].IsBorrowedByUser);
        Assert.Equal(2, result["book1"].ActiveLoanCount);
        Assert.Equal(0, result["book1"].AvailableCount);
    }

    [Fact]
    public async Task GetBorrowStatusBatchAsync_NonExistentBooks_SkipsThem()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetBorrowStatusBatchAsync(new List<string> { "book1", "nonexistent" }, 1);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.ContainsKey("book1"));
        Assert.False(result.ContainsKey("nonexistent"));
    }

    [Fact]
    public async Task GetUserLoanHistoryAsync_WithReturnedLoan_ReturnsHistoryWithReturnedDate()
    {
        _dbContext.Books.Add(new Book { Id = "book1", Name = "Test Book", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        var loan = new BookLoan { Id = "loan1", BookId = "book1", UserId = 1, BorrowedDate = DateTime.UtcNow.AddDays(-5), ReturnedDate = DateTime.UtcNow.AddDays(-2) };
        _dbContext.BookLoans.Add(loan);
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var result = await service.GetUserLoanHistoryAsync(1, 1, 10);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.True(result.Items[0].IsReturned);
        Assert.NotNull(result.Items[0].ReturnedDate);
        Assert.Equal("Test Book", result.Items[0].Name);
    }

    [Fact]
    public async Task GetUserLoanHistoryAsync_EmptyHistory_ReturnsEmptyPage()
    {
        var service = CreateService();
        var result = await service.GetUserLoanHistoryAsync(1, 1, 10);

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetUserLoanHistoryAsync_Pagination_ReturnsCorrectPage()
    {
        _dbContext.Books.Add(new Book { Id = "b1", Name = "Book 1", Author = "A", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "b2", Name = "Book 2", Author = "A", ISBN = "9781566199094", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "b3", Name = "Book 3", Author = "A", ISBN = "9780321349606", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.BookLoans.Add(new BookLoan { Id = "l1", BookId = "b1", UserId = 1, BorrowedDate = DateTime.UtcNow.AddDays(-10) });
        _dbContext.BookLoans.Add(new BookLoan { Id = "l2", BookId = "b2", UserId = 1, BorrowedDate = DateTime.UtcNow.AddDays(-5) });
        _dbContext.BookLoans.Add(new BookLoan { Id = "l3", BookId = "b3", UserId = 1, BorrowedDate = DateTime.UtcNow.AddDays(-1) });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var result = await service.GetUserLoanHistoryAsync(1, 2, 2);

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.True(result.TotalPages >= 1);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Single(result.Items);
        Assert.Equal("Book 1", result.Items[0].Name);
    }
}
