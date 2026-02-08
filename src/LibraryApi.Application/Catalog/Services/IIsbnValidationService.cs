namespace LibraryApi.Application.Catalog.Services;

public interface IIsbnValidationService
{
    bool ValidateIsbn13(string isbn);
}


