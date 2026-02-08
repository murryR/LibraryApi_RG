# LibraryApi - API Reference

## Overview

This document describes the REST API for programmatic access to LibraryApi. All endpoints require **API Key authentication** via the `X-API-Key` header.

**Base URL:** `https://your-domain.com/api/catalog-api`

**Authentication:** API Key (required in `X-API-Key` header)

---

## Authentication

All API requests must include an API key in the request header:

```
X-API-Key: your-api-key-here
```

**Note:** Contact your system administrator to obtain an API key. API keys are associated with API user accounts and have the same permissions as the user account.

---

## API Endpoints

### 1. Get All Books

Retrieve a paginated list of books with optional filtering.

**Endpoint:** `GET /api/catalog-api`

**Query Parameters:**
- `pageNumber` (integer, optional): Page number (default: 1)
- `pageSize` (integer, optional): Number of items per page (default: 10, max: 100)
- `search` (string, optional): Combined search filter. Comma-separated terms; each term matches name, author, or ISBN (case-insensitive partial match). All terms must match (AND logic). Example: `rowling, harry potter`
- `name` (string, optional): Filter by book name (used when `search` is not provided)
- `author` (string, optional): Filter by author name (used when `search` is not provided)
- `isbn` (string, optional): Filter by ISBN (used when `search` is not provided)

**Request Example:**
```bash
curl -X GET "https://your-domain.com/api/catalog-api?pageNumber=1&pageSize=10&search=harry,rowling" \
  -H "X-API-Key: your-api-key-here"
```

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Harry Potter and the Philosopher's Stone",
      "author": "J.K. Rowling",
      "issueYear": 1997,
      "isbn": "978-0-7475-3269-9",
      "numberOfPieces": 5
    }
  ],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**Response Fields:**
- `items`: Array of book objects
- `totalCount`: Total number of books matching the filters
- `pageNumber`: Current page number
- `pageSize`: Number of items per page
- `totalPages`: Total number of pages
- `hasPreviousPage`: Boolean indicating if previous page exists
- `hasNextPage`: Boolean indicating if next page exists

**Book Object Fields:**
- `id`: Unique book identifier (GUID)
- `name`: Book title
- `author`: Author name
- `issueYear`: Publication year
- `isbn`: ISBN-13 identifier
- `numberOfPieces`: Total number of copies available

**Error Responses:**
- `401 Unauthorized`: Missing or invalid API key
- `500 Internal Server Error`: Server error

---

### 2. Add New Book

Create a new book in the library catalog.

**Endpoint:** `POST /api/catalog-api`

**Request Body:**
```json
{
  "name": "Book Title",
  "author": "Author Name",
  "issueyear": 2024,
  "isbn": "978-0-123456-78-9",
  "numberOfPieces": 5
}
```

**Request Fields:**
- `name` (string, required): Book title (max 300 characters)
- `author` (string, required): Author name (max 200 characters)
- `issueyear` (integer, required): Publication year (between 1000 and 2100)
- `isbn` (string, required): ISBN-13 identifier (max 50 characters, must be valid ISBN-13)
- `numberOfPieces` (integer, required): Number of copies available (must be >= 0)

**Request Example:**
```bash
curl -X POST "https://your-domain.com/api/catalog-api" \
  -H "X-API-Key: your-api-key-here" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "The Great Gatsby",
    "author": "F. Scott Fitzgerald",
    "issueyear": 1925,
    "isbn": "978-0-7432-7356-5",
    "numberOfPieces": 3
  }'
```

**Success Response:** `201 Created`
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "The Great Gatsby",
  "author": "F. Scott Fitzgerald",
  "issueYear": 1925,
  "isbn": "978-0-7432-7356-5",
  "numberOfPieces": 3
}
```

**Error Responses:**
- `400 Bad Request`: Validation errors (invalid ISBN, missing required fields, etc.)
- `409 Conflict`: Book already exists (Name + Author + ISBN combination must be unique)
- `401 Unauthorized`: Missing or invalid API key
- `500 Internal Server Error`: Server error

**Validation Rules:**
- ISBN must be valid ISBN-13 format (with or without hyphens)
- Name + Author + ISBN combination must be unique
- Issue year must be between 1000 and 2100
- Number of pieces must be >= 0

---

### 3. Borrow a Book

**Endpoint:** `POST /api/catalog-api/{bookId}/borrow`

**URL Parameters:**
- `bookId` (string, required): Unique book identifier (GUID)

**Success Response:** `200 OK`
```json
{
  "loanId": "660e8400-e29b-41d4-a716-446655440001",
  "bookId": "550e8400-e29b-41d4-a716-446655440000",
  "userId": 1,
  "borrowedDate": "2024-01-15T10:30:00Z",
  "message": "Book borrowed successfully"
}
```

**Error Responses:**
- `400 Bad Request`: Invalid book ID format
- `404 Not Found`: Book with the specified ID does not exist
- `409 Conflict`: No available copies
- `401 Unauthorized`: Missing or invalid API key
- `500 Internal Server Error`: Server error

**Notes:**
- The user ID is automatically determined from the API key
- Multiple loans per user for the same book are allowed
- Availability is calculated as: `NumberOfPieces - ActiveLoanCount`

---

### 4. Return a Book

**Endpoint:** `POST /api/catalog-api/{bookId}/return`

**URL Parameters:**
- `bookId` (string, required): Unique book identifier (GUID)

**Success Response:** `200 OK`
```json
{
  "message": "Book returned successfully"
}
```

**Error Responses:**
- `400 Bad Request`: Invalid book ID or user doesn't have an active loan for this book
- `404 Not Found`: Book with the specified ID does not exist
- `401 Unauthorized`: Missing or invalid API key
- `500 Internal Server Error`: Server error

**Notes:**
- The user ID is automatically determined from the API key
- If a user has multiple loans for the same book, the oldest loan is returned first (FIFO)

---

## Error Handling

### HTTP Status Codes

- `200 OK`: Request successful
- `201 Created`: Resource created successfully
- `400 Bad Request`: Validation error or invalid request
- `401 Unauthorized`: Authentication required or invalid API key
- `404 Not Found`: Resource not found
- `409 Conflict`: Conflict (e.g., duplicate book or no available copies)
- `500 Internal Server Error`: Server error

### Error Response Format

All error responses follow this format:
```json
{
  "message": "Error description"
}
```

Validation errors may include additional details in an `errors` array.

---

## Best Practices

1. **API Key Security**: Never share your API key; store it securely (e.g. environment variables).
2. **Rate Limiting**: Be mindful of request frequency; use exponential backoff for retries.
3. **Error Handling**: Always check HTTP status codes; handle 409 for borrow (no copies).
4. **Pagination**: Use pagination for large result sets; maximum page size is 100.
5. **ISBN Format**: ISBN-13 required; hyphens optional.

---

## Example Workflows

### Add and Borrow a Book

```bash
# 1. Add a new book
curl -X POST "https://your-domain.com/api/catalog-api" \
  -H "X-API-Key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{"name": "Example Book", "author": "Example Author", "issueyear": 2024, "isbn": "978-0-123456-78-9", "numberOfPieces": 2}'

# 2. Borrow the book (use book ID from response)
curl -X POST "https://your-domain.com/api/catalog-api/<bookId>/borrow" \
  -H "X-API-Key: your-api-key"

# 3. Return the book
curl -X POST "https://your-domain.com/api/catalog-api/<bookId>/return" \
  -H "X-API-Key: your-api-key"
```

### Search and Filter Books

```bash
curl -X GET "https://your-domain.com/api/catalog-api?name=harry&pageSize=20" \
  -H "X-API-Key: your-api-key"
```

---

For full technical details (layers, configuration, database), see [ARCHITECTURE.md](ARCHITECTURE.md).
