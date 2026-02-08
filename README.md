# LibraryApi

A web-based library application built on .NET 10 for managing books, loans, and returns. It implements the technical assignment: books (ID, name, author, year, ISBN, copy count); list, add, search, borrow, return; Blazor UI and REST API; SQLite.

This solution is inspired by common Clean Architecture examples but implemented independently (own structure, naming, API error handling, and extensions).

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Running Locally

```bash
cd src/LibraryApi.Web
dotnet run
```

The app runs at `https://localhost:7xxx` or `http://localhost:5xxx` (see console). On first run, migrations and seed data (users, books) are applied.

## What's in the Project

- **Blazor Server** – Book list, filters (name, author, ISBN), add book, borrow/return, “My borrowed books”.
- **REST API** – Catalog: `GET/POST /api/catalog`, `POST /api/catalog/{id}/borrow`, `POST /api/catalog/{id}/return`. Cookie auth for the web UI; API key (`X-API-Key`) for external systems at `/api/catalog-api`.
- **Health** – `GET /Health` returns `{ "status": "Healthy", "database": "Ok", "timestamp": "..." }`.
- **SQLite** – Database in `app.db` (path from connection string).
- **Documentation** – [docs/](docs/) (architecture, API, UI, Azure deployment).

## Solution Structure

- **src/LibraryApi.Domain** – Entities (Book, BookLoan, ApplicationUser).
- **src/LibraryApi.Application** – Catalog and loan services, DTOs, validators, repository interfaces (`IBookCatalogRepository`, `ILoanRepository`).
- **src/LibraryApi.Infrastructure** – EF Core, SQLite, repositories, migrations, seeders.
- **src/LibraryApi.Web** – Blazor pages, API controllers (Catalog, CatalogApi, Auth, Status, Health), authentication, middleware.
- **tests/LibraryApi.UnitTests** – Unit tests for catalog, loan, auth, validators.

## Checklist

- Book: ID (GUID), Název, Autor, Rok vydání, ISBN, Počet kusů  
- List all books, add book, search (name/author/ISBN), borrow, return  
- Data: SQLite  
- UI (Blazor) + API for external systems  
- Production-quality code, validation, tests, security (Cookie + API Key), loan history (“My borrowed books”)

## AI / Tools Used

This project was developed with **Cursor** (AI-assisted editing). AI was used for implementation scaffolding, tests, documentation, and refactoring. Architecture decisions, layer boundaries, and .cursorrules were defined by the developer; AI assisted with boilerplate, naming consistency, and documentation text.

## Notes

- **Login**: Use seeded users (see `UserSeeder` or docs).
- **API key**: Configure in `appsettings`; required for `/api/catalog-api`.
- **Production**: Azure App Service with SQLite on persistent storage; see [docs/DEPLOYMENT_AZURE.md](docs/DEPLOYMENT_AZURE.md).

## Code Highlights

- **Repository pattern** – `IBookCatalogRepository`, `ILoanRepository` in Application; implementations in Infrastructure.
- **API errors** – `ApiExceptionMiddleware` maps exceptions on `/api/*` to RFC 7807 Problem Details (JSON).
- **Startup** – Logging and data protection are configured via extension methods (`AddLibraryApiLogging`, `AddLibraryApiDataProtection`).
