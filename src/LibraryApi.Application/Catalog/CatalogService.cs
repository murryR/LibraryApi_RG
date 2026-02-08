using Microsoft.Extensions.Logging;
using LibraryApi.Application.Catalog.Dtos;
using LibraryApi.Application.Common.Dtos;
using LibraryApi.Application.Common.Interfaces;
using LibraryApi.Domain.Entities;

namespace LibraryApi.Application.Catalog;

public class CatalogService : ICatalogService
{
    private readonly IBookCatalogRepository _catalogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(IBookCatalogRepository catalogRepository, IUnitOfWork unitOfWork, ILogger<CatalogService> logger)
    {
        _catalogRepository = catalogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResult<BookDto>> ListBooksAsync(ListBooksRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var pageNumber = Math.Max(1, request.PageNumber);
            var pageSize = Math.Max(1, Math.Min(100, request.PageSize));

            IReadOnlyList<string>? searchTerms = null;
            string? name = null;
            string? author = null;
            string? isbn = null;

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                searchTerms = request.Search
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .ToList();
            }

            if (searchTerms == null || searchTerms.Count == 0)
            {
                name = request.Name;
                author = request.Author;
                isbn = request.ISBN;
            }

            var (items, totalCount) = await _catalogRepository.GetFilteredAsync(
                pageNumber, pageSize, name, author, isbn,
                request.SortBy, request.SortDirection, request.OnlyAvailable, searchTerms, cancellationToken);

            var bookDtos = items.Select(b => MapToDto(b)).ToList();

            return new PagedResult<BookDto>
            {
                Items = bookDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing books");
            throw;
        }
    }

    public async Task<BookDto> CreateBookAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _catalogRepository.ExistsNameAuthorIsbnAsync(request.name, request.author, request.isbn, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("Duplicate book: Name={Name}, Author={Author}, ISBN={ISBN}",
                    request.name, request.author, request.isbn);
                throw new InvalidOperationException(
                    $"A book with the combination of Name '{request.name}', Author '{request.author}', and ISBN '{request.isbn}' already exists.");
            }

            var book = new Book
            {
                Name = request.name,
                Author = request.author,
                IssueYear = request.issueyear,
                ISBN = request.isbn,
                NumberOfPieces = request.numberOfPieces
            };

            _catalogRepository.Add(book);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Book created: Id={Id}, Name={Name}", book.Id, book.Name);

            return MapToDto(book);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding book: Name={Name}, Author={Author}, ISBN={ISBN}",
                request.name, request.author, request.isbn);
            throw;
        }
    }

    public async Task<List<string>> GetBookNameSuggestionsAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _catalogRepository.GetBookNameSuggestionsAsync(prefix, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting book name suggestions for prefix: {Prefix}", prefix);
            throw;
        }
    }

    public async Task<List<string>> GetAuthorSuggestionsAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _catalogRepository.GetAuthorSuggestionsAsync(prefix, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting author suggestions for prefix: {Prefix}", prefix);
            throw;
        }
    }

    private static BookDto MapToDto(Book b)
    {
        return new BookDto
        {
            Id = b.Id,
            Name = b.Name,
            Author = b.Author,
            IssueYear = b.IssueYear,
            ISBN = b.ISBN,
            NumberOfPieces = b.NumberOfPieces
        };
    }
}
