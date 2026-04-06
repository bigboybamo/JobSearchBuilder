# /add-query

Scaffold a new CQRS Query — record, handler, and response DTO — following project conventions.

## Usage
```
/add-query <GetNoun> [ById|ByFilter|List]
```

## Examples
```
/add-query GetOrderById
/add-query GetProductList
/add-query GetInvoicesByCustomer
```

## Instructions

You are scaffolding a CQRS Query with three files. Generate them in this order.

### Target folder
`src/{Project}.Application/Queries/{GetNoun}/`

---

### File 1 — Query record
**Path:** `Queries/{GetNoun}/{GetNoun}Query.cs`

```csharp
namespace {Project}.Application.Queries.{GetNoun};

public sealed record {GetNoun}Query(
    // TODO: add parameters used to filter or identify the resource
    // Single-item example: Guid OrderId
    // List example:        int PageNumber, int PageSize, string? Status
) : IRequest<Result<{GetNoun}Response>>;
// For lists use: IRequest<Result<IReadOnlyList<{Noun}Response>>>
```

---

### File 2 — Handler
**Path:** `Queries/{GetNoun}/{GetNoun}QueryHandler.cs`

```csharp
namespace {Project}.Application.Queries.{GetNoun};

public sealed class {GetNoun}QueryHandler : IRequestHandler<{GetNoun}Query, Result<{GetNoun}Response>>
{
    // TODO: inject read-side repository or DbContext (read-only is fine for queries)
    // Example: private readonly I{Noun}Repository _repository;

    public {GetNoun}QueryHandler(/* TODO: inject dependencies */)
    {
        // TODO: assign injected dependencies
    }

    public async Task<Result<{GetNoun}Response>> Handle(
        {GetNoun}Query request,
        CancellationToken cancellationToken)
    {
        // TODO: implement query logic
        // Pattern to follow:
        //   1. Fetch data from repository (no tracking needed for queries)
        //   2. Return Result.Failure("Not found") if resource does not exist
        //   3. Map to response DTO
        //   4. Return Result.Success(response)
        throw new NotImplementedException();
    }
}
```

---

### File 3 — Response DTO
**Path:** `Queries/{GetNoun}/{GetNoun}Response.cs`

```csharp
namespace {Project}.Application.Queries.{GetNoun};

public sealed record {GetNoun}Response(
    // TODO: add properties that the caller needs — never expose domain internals directly
    // Example: Guid Id, string OrderNumber, string Status, DateTimeOffset CreatedAt
);
```

---

### Step 3 — Remind the user what to do next

```
Next steps:
[ ] Fill in Query parameters
[ ] Fill in Response DTO properties
[ ] Fill in Handler — fetch, check existence, map, return Result
[ ] Write unit tests for the handler — run /write-tests {GetNoun}QueryHandler
[ ] Wire up the matching GET controller action — run /scaffold-endpoint if not done
```

## Rules
- Queries must never modify state. No `SaveChangesAsync()`, no domain method calls that mutate.
- Use `AsNoTracking()` on EF Core queries — reads don't need change tracking.
- Return a DTO, not a domain entity. The Application layer controls what is exposed.
- Validators are optional for queries but add one if the query has non-trivial input (e.g. pagination bounds, date range validation).
- For list queries, always include pagination parameters (`PageNumber`, `PageSize`) unless the dataset is guaranteed small.
