namespace LibraryApi.Application.Catalog.Dtos;

public class ListBooksRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    /// <summary>Comma-separated search terms; each term matches Name, Author, or ISBN (partial match). All terms must match (AND).</summary>
    public string? Search { get; set; }
    public string? Name { get; set; }
    public string? Author { get; set; }
    public string? ISBN { get; set; }
    public string? SortBy { get; set; }  // "Name" or "Author"
    public string? SortDirection { get; set; }  // "asc" or "desc"
    public bool OnlyAvailable { get; set; }
}


