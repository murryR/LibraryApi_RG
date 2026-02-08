using FluentValidation.TestHelper;
using LibraryApi.Application.Catalog.Dtos;
using LibraryApi.Application.Catalog.Validators;

namespace LibraryApi.UnitTests.Validators;

public class ReturnBookRequestValidatorTests
{
    private readonly ReturnRequestValidator _validator;

    public ReturnBookRequestValidatorTests()
    {
        _validator = new ReturnRequestValidator();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BookIdIsEmpty_ReturnsValidationError(string? bookId)
    {
        // Arrange
        var request = new ReturnRequest
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
        var request = new ReturnRequest
        {
            BookId = "book-id-123"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}


