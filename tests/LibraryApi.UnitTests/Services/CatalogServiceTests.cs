using LibraryApi.Application.Catalog;
using LibraryApi.Application.Catalog.Dtos;
using LibraryApi.Application.Common.Interfaces;
using LibraryApi.Domain.Entities;
using LibraryApi.Infrastructure.Persistence;
using LibraryApi.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryApi.UnitTests.Services;

public class CatalogServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly IBookCatalogRepository _catalogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CatalogService> _logger;

    public CatalogServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new AppDbContext(options);
        _catalogRepository = new BookCatalogRepository(_dbContext);
        _unitOfWork = _dbContext;
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CatalogService>.Instance;
    }

    private ICatalogService CreateService() => new CatalogService(_catalogRepository, _unitOfWork, _logger);

    [Fact]
    public async Task ListBooksAsync_NoFilters_ReturnsAllBooks()
    {
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var request = new ListBooksRequest { PageNumber = 1, PageSize = 10 };

        var result = await service.ListBooksAsync(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ListBooksAsync_WithNameFilter_ReturnsFilteredBooks()
    {
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var request = new ListBooksRequest { PageNumber = 1, PageSize = 10, Name = "Book 1" };

        var result = await service.ListBooksAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Book 1", result.Items[0].Name);
    }

    [Fact]
    public async Task ListBooksAsync_WithAuthorFilter_ReturnsFilteredBooks()
    {
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var request = new ListBooksRequest { PageNumber = 1, PageSize = 10, Author = "Author 1" };

        var result = await service.ListBooksAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Author 1", result.Items[0].Author);
    }

    [Fact]
    public async Task ListBooksAsync_WithIsbnFilter_ReturnsFilteredBooks()
    {
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var request = new ListBooksRequest { PageNumber = 1, PageSize = 10, ISBN = "9781566199094" };

        var result = await service.ListBooksAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("9781566199094", result.Items[0].ISBN);
    }

    [Fact]
    public async Task ListBooksAsync_WithSearchFilter_CommaSeparatedTerms_ReturnsFilteredBooks()
    {
        _dbContext.Books.Add(new Book { Id = "1", Name = "Harry Potter", Author = "J.K. Rowling", ISBN = "9780747532699", IssueYear = 1997, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Cursed Child", Author = "J.K. Rowling", ISBN = "9780751565355", IssueYear = 2016, NumberOfPieces = 3 });
        _dbContext.Books.Add(new Book { Id = "3", Name = "Other Book", Author = "Different Author", ISBN = "9781234567890", IssueYear = 2020, NumberOfPieces = 1 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var request = new ListBooksRequest { PageNumber = 1, PageSize = 10, Search = "rowling, harry" };

        var result = await service.ListBooksAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Harry Potter", result.Items[0].Name);
        Assert.Equal("J.K. Rowling", result.Items[0].Author);
    }

    [Fact]
    public async Task ListBooksAsync_WithSearchFilter_PartialMatch_ReturnsMatchingBooks()
    {
        _dbContext.Books.Add(new Book { Id = "1", Name = "Harry Potter and the Philosopher's Stone", Author = "J.K. Rowling", ISBN = "9780747532699", IssueYear = 1997, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book 2", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var request = new ListBooksRequest { PageNumber = 1, PageSize = 10, Search = "harry" };

        var result = await service.ListBooksAsync(request);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Contains("Harry", result.Items[0].Name);
    }

    [Fact]
    public async Task ListBooksAsync_WithPagination_ReturnsPaginatedResults()
    {
        for (int i = 1; i <= 3; i++)
            _dbContext.Books.Add(new Book { Id = i.ToString(), Name = $"Book {i}", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var request = new ListBooksRequest { PageNumber = 1, PageSize = 2 };

        var result = await service.ListBooksAsync(request);

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task ListBooksAsync_PageNumberZero_NormalizesToPage1()
    {
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var request = new ListBooksRequest { PageNumber = 0, PageSize = 10 };

        var result = await service.ListBooksAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
    }

    [Fact]
    public async Task ListBooksAsync_PageSizeZero_NormalizesToPageSize1()
    {
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var request = new ListBooksRequest { PageNumber = 1, PageSize = 0 };

        var result = await service.ListBooksAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.PageSize);
    }

    [Fact]
    public async Task ListBooksAsync_PageSizeExceedsMax_NormalizesTo100()
    {
        for (int i = 1; i <= 150; i++)
            _dbContext.Books.Add(new Book { Id = i.ToString(), Name = $"Book {i}", Author = "Author", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var request = new ListBooksRequest { PageNumber = 1, PageSize = 200 };

        var result = await service.ListBooksAsync(request);

        Assert.NotNull(result);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(100, result.Items.Count);
    }

    [Fact]
    public async Task CreateBookAsync_ValidBook_ReturnsCreatedBook()
    {
        var service = CreateService();
        var request = new CreateBookRequest
        {
            name = "Test Book",
            author = "Test Author",
            issueyear = 2023,
            isbn = "9780131101630",
            numberOfPieces = 5
        };

        var result = await service.CreateBookAsync(request);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("Test Book", result.Name);
        Assert.Equal("Test Author", result.Author);
        Assert.Equal(2023, result.IssueYear);
        Assert.Equal("9780131101630", result.ISBN);
        Assert.Equal(5, result.NumberOfPieces);
    }

    [Fact]
    public async Task CreateBookAsync_DuplicateBook_ThrowsInvalidOperationException()
    {
        _dbContext.Books.Add(new Book { Id = "1", Name = "Test Book", Author = "Test Author", ISBN = "9780131101630", IssueYear = 2023, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();
        var request = new CreateBookRequest
        {
            name = "Test Book",
            author = "Test Author",
            issueyear = 2023,
            isbn = "9780131101630",
            numberOfPieces = 5
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateBookAsync(request));
    }

    [Fact]
    public async Task GetBookNameSuggestionsAsync_WithPrefix_ReturnsSuggestions()
    {
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book One", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book Two", Author = "Author 2", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        _dbContext.Books.Add(new Book { Id = "3", Name = "Article Three", Author = "Author 3", ISBN = "9781234567890", IssueYear = 2022, NumberOfPieces = 10 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetBookNameSuggestionsAsync("Book");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("Book One", result);
        Assert.Contains("Book Two", result);
    }

    [Fact]
    public async Task GetBookNameSuggestionsAsync_EmptyPrefix_ReturnsEmptyList()
    {
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book One", Author = "Author 1", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetBookNameSuggestionsAsync("");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAuthorSuggestionsAsync_WithPrefix_ReturnsSuggestions()
    {
        _dbContext.Books.Add(new Book { Id = "1", Name = "Book 1", Author = "John Doe", ISBN = "9780131101630", IssueYear = 2020, NumberOfPieces = 5 });
        _dbContext.Books.Add(new Book { Id = "2", Name = "Book 2", Author = "John Smith", ISBN = "9781566199094", IssueYear = 2021, NumberOfPieces = 3 });
        _dbContext.Books.Add(new Book { Id = "3", Name = "Book 3", Author = "Jane Doe", ISBN = "9781234567890", IssueYear = 2022, NumberOfPieces = 10 });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        var result = await service.GetAuthorSuggestionsAsync("John");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("John Doe", result);
        Assert.Contains("John Smith", result);
    }
}
