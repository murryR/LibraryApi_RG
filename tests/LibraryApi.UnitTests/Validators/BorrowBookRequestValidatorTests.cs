using FluentValidation.TestHelper;
using LibraryApi.Application.Catalog.Dtos;
using LibraryApi.Application.Catalog.Validators;

namespace LibraryApi.UnitTests.Validators;

public class BorrowBookRequestValidatorTests
{
    private readonly BorrowRequestValidator _validator;

    public BorrowBookRequestValidatorTests()
    {
        _validator = new BorrowRequestValidator();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BookIdIsEmpty_ReturnsValidationError(string? bookId)
    {
        // Arrange
        var request = new BorrowRequest
        {
            BookId = bookId ?? ""
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BookId).WithErrorMessage("BookId is required");
    }

    [Fact]
    public void Validate_ValidBookId_ReturnsNoErrors()
    {
        // Arrange
        var request = new BorrowRequest
        {
            BookId = "book-id-123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}


