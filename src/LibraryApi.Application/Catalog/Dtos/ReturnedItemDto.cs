using System.Text.Json.Serialization;

namespace LibraryApi.Application.Catalog.Dtos;

public class ReturnedItemDto
{
    [JsonPropertyName("loanId")]
    public string LoanId { get; set; } = string.Empty;

    [JsonPropertyName("bookId")]
    public string BookId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("issueYear")]
    public int IssueYear { get; set; }

    [JsonPropertyName("isbn")]
    public string ISBN { get; set; } = string.Empty;

    [JsonPropertyName("returnedDate")]
    public DateTime ReturnedDate { get; set; }
}
