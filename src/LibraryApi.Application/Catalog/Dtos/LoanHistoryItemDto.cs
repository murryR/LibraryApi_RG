using System.Text.Json.Serialization;

namespace LibraryApi.Application.Catalog.Dtos;

/// <summary>
/// DTO for a single item in a user's loan history (active or returned).
/// </summary>
public class LoanHistoryItemDto
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

    [JsonPropertyName("borrowedDate")]
    public DateTime BorrowedDate { get; set; }

    [JsonPropertyName("returnedDate")]
    public DateTime? ReturnedDate { get; set; }

    [JsonPropertyName("isReturned")]
    public bool IsReturned => ReturnedDate.HasValue;
}
