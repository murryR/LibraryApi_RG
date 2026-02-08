using System.Text.Json.Serialization;

namespace LibraryApi.Application.Catalog.Dtos;

public class BorrowResult
{
    [JsonPropertyName("loanId")]
    public string LoanId { get; set; } = string.Empty;

    [JsonPropertyName("bookId")]
    public string BookId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("borrowedDate")]
    public DateTime BorrowedDate { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}


