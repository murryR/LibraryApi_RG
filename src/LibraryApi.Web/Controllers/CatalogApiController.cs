using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LibraryApi.Application.Catalog;
using LibraryApi.Application.Catalog.Dtos;
using LibraryApi.Application.Catalog.Validators;
using LibraryApi.Application.Common.Dtos;

namespace LibraryApi.Web.Controllers;

/// <summary>
/// External API for catalog and loans - requires API Key authentication.
/// </summary>
[ApiController]
[Route("api/catalog-api")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class CatalogApiController : ControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly ILoanService _loanService;
    private readonly ILogger<CatalogApiController> _logger;
    private readonly IValidator<BorrowRequest> _borrowValidator;
    private readonly IValidator<ReturnRequest> _returnValidator;

    public CatalogApiController(
        ICatalogService catalogService,
        ILoanService loanService,
        ILogger<CatalogApiController> logger,
        IValidator<BorrowRequest> borrowValidator,
        IValidator<ReturnRequest> returnValidator)
    {
        _catalogService = catalogService;
        _loanService = loanService;
        _logger = logger;
        _borrowValidator = borrowValidator;
        _returnValidator = returnValidator;
    }

    [HttpGet]
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
    public async Task<ActionResult<BookDto>> CreateBook(
        [FromBody] CreateBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _catalogService.CreateBookAsync(request, cancellationToken);
        _logger.LogInformation("Book created via API - Id: {Id}", result.Id);
        return CreatedAtAction(nameof(ListBooks), new { id = result.Id }, result);
    }

    [HttpPost("{bookId}/borrow")]
    public async Task<ActionResult<BorrowResult>> BorrowBook(string bookId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookId))
            return BadRequest(new { message = "BookId is required" });

        var userIdClaim = User.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "User authentication required" });

        var request = new BorrowRequest { BookId = bookId };
        var validationResult = await _borrowValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });

        var result = await _loanService.BorrowBookAsync(bookId, userId, cancellationToken);
        _logger.LogInformation("Book borrowed via API - BookId: {BookId}, LoanId: {LoanId}", bookId, result.LoanId);
        return Ok(result);
    }

    [HttpPost("{bookId}/return")]
    public async Task<ActionResult> ReturnBook(string bookId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookId))
            return BadRequest(new { message = "BookId is required" });

        var userIdClaim = User.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "User authentication required" });

        var request = new ReturnRequest { BookId = bookId };
        var validationResult = await _returnValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(new { message = "Validation failed", errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });

        await _loanService.ReturnBookAsync(bookId, userId, cancellationToken);
        _logger.LogInformation("Book returned via API - BookId: {BookId}", bookId);
        return Ok(new { message = "Book returned successfully" });
    }

    /// <summary>
    /// Get current API user's loan history (all borrows including returned), paginated.
    /// </summary>
    [HttpGet("me/loan-history")]
    public async Task<ActionResult<PagedResult<LoanHistoryItemDto>>> GetMyLoanHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "User authentication required" });

        var result = await _loanService.GetUserLoanHistoryAsync(userId, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }
}
