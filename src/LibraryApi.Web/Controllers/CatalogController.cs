using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LibraryApi.Application.Catalog;
using LibraryApi.Application.Catalog.Dtos;
using LibraryApi.Application.Catalog.Services;
using LibraryApi.Application.Catalog.Validators;
using LibraryApi.Application.Common.Dtos;

namespace LibraryApi.Web.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly ILoanService _loanService;
    private readonly ILogger<CatalogController> _logger;
    private readonly IValidator<CreateBookRequest> _validator;
    private readonly IValidator<BorrowRequest> _borrowValidator;
    private readonly IValidator<ReturnRequest> _returnValidator;
    private readonly IIsbnValidationService _isbnValidationService;

    public CatalogController(
        ICatalogService catalogService,
        ILoanService loanService,
        ILogger<CatalogController> logger,
        IValidator<CreateBookRequest> validator,
        IValidator<BorrowRequest> borrowValidator,
        IValidator<ReturnRequest> returnValidator,
        IIsbnValidationService isbnValidationService)
    {
        _catalogService = catalogService;
        _loanService = loanService;
        _logger = logger;
        _validator = validator;
        _borrowValidator = borrowValidator;
        _returnValidator = returnValidator;
        _isbnValidationService = isbnValidationService;
    }

    /// <summary>
    /// Public (anonymous) book list: name, author, ISBN, available count only.
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<PublicBookDto>>> ListBooksPublic(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? name = null,
        [FromQuery] string? author = null,
        [FromQuery] string? isbn = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] bool onlyAvailable = false,
        CancellationToken cancellationToken = default)
    {
        var request = new ListBooksRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search,
            Name = name,
            Author = author,
            ISBN = isbn,
            SortBy = sortBy,
            SortDirection = sortDirection,
            OnlyAvailable = onlyAvailable
        };
        var result = await _catalogService.ListBooksAsync(request, cancellationToken);
        var bookIds = result.Items.Select(b => b.Id).ToList();
        var statuses = bookIds.Count > 0
            ? await _loanService.GetBorrowStatusBatchAsync(bookIds, 0, cancellationToken)
            : new Dictionary<string, BorrowStatusDto>();
        var publicItems = result.Items.Select(b =>
        {
            var status = statuses.GetValueOrDefault(b.Id);
            var available = status != null ? status.AvailableCount : b.NumberOfPieces;
            return new PublicBookDto
            {
                Name = b.Name,
                Author = b.Author,
                ISBN = b.ISBN,
                AvailableCount = available
            };
        }).ToList();
        return Ok(new PagedResult<PublicBookDto>
        {
            Items = publicItems,
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        });
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<PagedResult<BookDto>>> ListBooks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? name = null,
        [FromQuery] string? author = null,
        [FromQuery] string? isbn = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] bool onlyAvailable = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ListBooks - PageNumber: {PageNumber}, PageSize: {PageSize}, Search: {Search}, Name: {Name}, Author: {Author}, ISBN: {ISBN}, SortBy: {SortBy}, SortDirection: {SortDirection}, OnlyAvailable: {OnlyAvailable}",
            pageNumber, pageSize, search, name, author, isbn, sortBy, sortDirection, onlyAvailable);

        var request = new ListBooksRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Search = search,
            Name = name,
            Author = author,
            ISBN = isbn,
            SortBy = sortBy,
            SortDirection = sortDirection,
            OnlyAvailable = onlyAvailable
        };

        var result = await _catalogService.ListBooksAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BookDto>> CreateBook(
        [FromBody] CreateBookRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            _logger.LogWarning("CreateBook: Request body is null");
            return BadRequest(new { message = "Request body is required and must be valid JSON" });
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("CreateBook: Validation failed. Errors: {Errors}",
                string.Join("; ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }

        var result = await _catalogService.CreateBookAsync(request, cancellationToken);
        _logger.LogInformation("Book created - Id: {Id}", result.Id);
        return CreatedAtAction(nameof(ListBooks), new { id = result.Id }, result);
    }

    [HttpGet("validate-isbn")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public ActionResult<bool> ValidateIsbn([FromQuery] string isbn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return Ok(false);
        var isValid = _isbnValidationService.ValidateIsbn13(isbn);
        return Ok(isValid);
    }

    [HttpGet("name-suggestions")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<List<string>>> GetBookNameSuggestions(
        [FromQuery] string prefix,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 4)
            return Ok(new List<string>());
        var suggestions = await _catalogService.GetBookNameSuggestionsAsync(prefix, cancellationToken);
        return Ok(suggestions);
    }

    [HttpGet("author-suggestions")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<List<string>>> GetAuthorSuggestions(
        [FromQuery] string prefix,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 4)
            return Ok(new List<string>());
        var suggestions = await _catalogService.GetAuthorSuggestionsAsync(prefix, cancellationToken);
        return Ok(suggestions);
    }

    [HttpPost("{bookId}/borrow")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BorrowResult>> BorrowBook(string bookId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookId))
            return BadRequest(new { message = "BookId is required" });

        var userIdClaim = HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("BorrowBook: User not authenticated or invalid UserId claim");
            return Unauthorized(new { message = "User authentication required" });
        }

        var request = new BorrowRequest { BookId = bookId };
        var validationResult = await _borrowValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });

        var result = await _loanService.BorrowBookAsync(bookId, userId, cancellationToken);
        _logger.LogInformation("Book borrowed - BookId: {BookId}, UserId: {UserId}, LoanId: {LoanId}", bookId, userId, result.LoanId);
        return Ok(result);
    }

    [HttpPost("{bookId}/return")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult> ReturnBook(string bookId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookId))
            return BadRequest(new { message = "BookId is required" });

        var userIdClaim = HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("ReturnBook: User not authenticated or invalid UserId claim");
            return Unauthorized(new { message = "User authentication required" });
        }

        var request = new ReturnRequest { BookId = bookId };
        var validationResult = await _returnValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });

        await _loanService.ReturnBookAsync(bookId, userId, cancellationToken);
        _logger.LogInformation("Book returned - BookId: {BookId}, UserId: {UserId}", bookId, userId);
        return Ok(new { message = "Book returned successfully" });
    }

    [HttpGet("{bookId}/borrow-status")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<BorrowStatusDto>> GetBorrowStatus(string bookId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookId))
            return BadRequest(new { message = "BookId is required" });

        var userIdClaim = HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "User authentication required" });

        var result = await _loanService.GetBorrowStatusAsync(bookId, userId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("borrow-status/batch")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<Dictionary<string, BorrowStatusDto>>> GetBorrowStatusBatch(
        [FromBody] List<string> bookIds,
        CancellationToken cancellationToken = default)
    {
        if (bookIds == null || bookIds.Count == 0)
            return Ok(new Dictionary<string, BorrowStatusDto>());

        var userIdClaim = HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "User authentication required" });

        var result = await _loanService.GetBorrowStatusBatchAsync(bookIds, userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-borrowed-books")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<List<BorrowedItemDto>>> GetMyBorrowedBooks(CancellationToken cancellationToken = default)
    {
        var userIdClaim = HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "User authentication required" });

        var result = await _loanService.GetUserBorrowedBooksAsync(userId, cancellationToken);
        _logger.LogInformation("GetMyBorrowedBooks: {Count} items for user {UserId}", result.Count, userId);
        return Ok(result);
    }

    /// <summary>
    /// Get current user's loan history (all borrows including returned), paginated.
    /// </summary>
    [HttpGet("me/loan-history")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<PagedResult<LoanHistoryItemDto>>> GetMyLoanHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = HttpContext.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "User authentication required" });

        var result = await _loanService.GetUserLoanHistoryAsync(userId, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }
}
