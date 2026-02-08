using Microsoft.EntityFrameworkCore;
using LibraryApi.Application.Common.Interfaces;
using LibraryApi.Domain.Entities;
using LibraryApi.Infrastructure.Persistence;

namespace LibraryApi.Infrastructure.Persistence.Repositories;

public class BookCatalogRepository : IBookCatalogRepository
{
    private readonly AppDbContext _context;

    public BookCatalogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Book?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<(List<Book> Items, int TotalCount)> GetFilteredAsync(int pageNumber, int pageSize, string? name, string? author, string? isbn, string? sortBy, string? sortDirection, bool onlyAvailable, IReadOnlyList<string>? searchTerms = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Books.AsQueryable()
            .Where(b => !string.IsNullOrEmpty(b.Id)
                && !string.IsNullOrEmpty(b.Name)
                && !string.IsNullOrEmpty(b.Author)
                && !string.IsNullOrEmpty(b.ISBN));

        var terms = searchTerms?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!.Trim()).ToList();
        if (terms != null && terms.Count > 0)
        {
            foreach (var term in terms)
            {
                var pattern = $"%{term}%";
                query = query.Where(b =>
                    EF.Functions.Like(b.Name, pattern) ||
                    EF.Functions.Like(b.Author, pattern) ||
                    EF.Functions.Like(b.ISBN, pattern));
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(b => EF.Functions.Like(b.Name, $"%{name}%"));

            if (!string.IsNullOrWhiteSpace(author))
                query = query.Where(b => EF.Functions.Like(b.Author, $"%{author}%"));

            if (!string.IsNullOrWhiteSpace(isbn))
                query = query.Where(b => EF.Functions.Like(b.ISBN, $"%{isbn}%"));
        }

        if (onlyAvailable)
        {
            query = query.Where(b => b.NumberOfPieces > _context.BookLoans
                .Count(bl => bl.BookId == b.Id && bl.ReturnedDate == null));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNum = Math.Max(1, pageNumber);
        var size = Math.Max(1, Math.Min(100, pageSize));
        var skip = (pageNum - 1) * size;

        var isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var orderBy = (string.IsNullOrWhiteSpace(sortBy) ? "Name" : sortBy).Trim();

        if (string.Equals(orderBy, "Author", StringComparison.OrdinalIgnoreCase))
        {
            query = isDesc
                ? query.OrderByDescending(b => b.Author).ThenBy(b => b.Name)
                : query.OrderBy(b => b.Author).ThenBy(b => b.Name);
        }
        else
        {
            query = isDesc
                ? query.OrderByDescending(b => b.Name).ThenBy(b => b.Author)
                : query.OrderBy(b => b.Name).ThenBy(b => b.Author);
        }

        var items = await query
            .Skip(skip)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Add(Book book)
    {
        _context.Books.Add(book);
    }

    public async Task<bool> ExistsNameAuthorIsbnAsync(string name, string author, string isbn, CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .AnyAsync(b => b.Name == name && b.Author == author && b.ISBN == isbn, cancellationToken);
    }

    public async Task<List<string>> GetBookNameSuggestionsAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return new List<string>();

        return await _context.Books
            .Where(b => !string.IsNullOrEmpty(b.Name)
                && !string.IsNullOrEmpty(b.Id)
                && !string.IsNullOrEmpty(b.Author)
                && !string.IsNullOrEmpty(b.ISBN)
                && EF.Functions.Like(b.Name, $"{prefix}%"))
            .Select(b => b.Name)
            .Distinct()
            .OrderBy(n => n)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetAuthorSuggestionsAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return new List<string>();

        return await _context.Books
            .Where(b => !string.IsNullOrEmpty(b.Name)
                && !string.IsNullOrEmpty(b.Id)
                && !string.IsNullOrEmpty(b.Author)
                && !string.IsNullOrEmpty(b.ISBN)
                && EF.Functions.Like(b.Author, $"{prefix}%"))
            .Select(b => b.Author)
            .Distinct()
            .OrderBy(a => a)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Book>> GetByIdsAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
            return new List<Book>();

        return await _context.Books
            .Where(b => ids.Contains(b.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Books.AnyAsync(b => b.Id == id, cancellationToken);
    }
}

