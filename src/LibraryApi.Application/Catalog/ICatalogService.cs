using LibraryApi.Application.Catalog.Dtos;
using LibraryApi.Application.Common.Dtos;

namespace LibraryApi.Application.Catalog;

public interface ICatalogService
{
    Task<PagedResult<BookDto>> ListBooksAsync(ListBooksRequest request, CancellationToken cancellationToken = default);
    Task<BookDto> CreateBookAsync(CreateBookRequest request, CancellationToken cancellationToken = default);
    Task<List<string>> GetBookNameSuggestionsAsync(string prefix, CancellationToken cancellationToken = default);
    Task<List<string>> GetAuthorSuggestionsAsync(string prefix, CancellationToken cancellationToken = default);
}
