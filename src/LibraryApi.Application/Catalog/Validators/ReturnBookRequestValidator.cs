using FluentValidation;
using LibraryApi.Application.Catalog.Dtos;

namespace LibraryApi.Application.Catalog.Validators;

public class ReturnRequestValidator : AbstractValidator<ReturnRequest>
{
    public ReturnRequestValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("BookId is required");
    }
}


