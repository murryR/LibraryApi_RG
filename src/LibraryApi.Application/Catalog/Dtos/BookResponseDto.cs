namespace LibraryApi.Application.Catalog.Dtos;

public class BookDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int IssueYear { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public int NumberOfPieces { get; set; }
}


