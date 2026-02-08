using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LibraryApi.Application.Catalog.Dtos;

public class BorrowRequest
{
    [Required(ErrorMessage = "BookId is required")]
    [JsonPropertyName("bookId")]
    public string BookId { get; set; } = string.Empty;
}


