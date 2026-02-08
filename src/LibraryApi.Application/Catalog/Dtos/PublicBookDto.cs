namespace LibraryApi.Application.Catalog.Dtos;

/// <summary>
/// DTO for public (anonymous) book listing: name, author, ISBN, available count only.
/// </summary>
public class PublicBookDto
{
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int AvailableCount { get; set; }
}
