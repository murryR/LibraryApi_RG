using System.Text.Json.Serialization;

namespace LibraryApi.Application.Catalog.Dtos;

public class BorrowStatusDto
{
    [JsonPropertyName("bookId")]
    public string BookId { get; set; } = string.Empty;

    [JsonPropertyName("isBorrowedByUser")]
    public bool IsBorrowedByUser { get; set; }

    [JsonPropertyName("activeLoanCount")]
    public int ActiveLoanCount { get; set; }

    [JsonPropertyName("availableCount")]
    public int AvailableCount { get; set; }
}


