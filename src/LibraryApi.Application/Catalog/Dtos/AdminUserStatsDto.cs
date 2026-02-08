namespace LibraryApi.Application.Catalog.Dtos;

public class AdminUserStatsDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int BorrowedCount { get; set; }
    public int ReturnedCount { get; set; }
}
