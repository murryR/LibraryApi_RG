using FluentValidation;
using LibraryApi.Application.Catalog.Dtos;

namespace LibraryApi.Application.Catalog.Validators;

public class BorrowRequestValidator : AbstractValidator<BorrowRequest>
{
    public BorrowRequestValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("BookId is required");
    }
}


