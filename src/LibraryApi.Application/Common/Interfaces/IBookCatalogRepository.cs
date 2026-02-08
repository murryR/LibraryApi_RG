using LibraryApi.Domain.Entities;

namespace LibraryApi.Application.Common.Interfaces;

public interface IBookCatalogRepository
{
    Task<Book?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<(List<Book> Items, int TotalCount)> GetFilteredAsync(int pageNumber, int pageSize, string? name, string? author, string? isbn, string? sortBy, string? sortDirection, bool onlyAvailable, IReadOnlyList<string>? searchTerms = null, CancellationToken cancellationToken = default);
    void Add(Book book);
    Task<bool> ExistsNameAuthorIsbnAsync(string name, string author, string isbn, CancellationToken cancellationToken = default);
    Task<List<string>> GetBookNameSuggestionsAsync(string prefix, CancellationToken cancellationToken = default);
    Task<List<string>> GetAuthorSuggestionsAsync(string prefix, CancellationToken cancellationToken = default);
    Task<List<Book>> GetByIdsAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
}
