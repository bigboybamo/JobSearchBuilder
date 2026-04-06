# /add-command

Scaffold a new CQRS Command — record, handler, and validator — following project conventions.

## Usage
```
/add-command <VerbNoun>
```

## Examples
```
/add-command CreateOrder
/add-command CancelInvoice
/add-command UpdateProductPrice
```

## Instructions

You are scaffolding a CQRS Command with three files. Generate them in this order.

### Target folder
`src/{Project}.Application/Commands/{VerbNoun}/`

---

### File 1 — Command record
**Path:** `Commands/{VerbNoun}/{VerbNoun}Command.cs`

```csharp
namespace {Project}.Application.Commands.{VerbNoun};

public sealed record {VerbNoun}Command(
    // TODO: add properties — use primitive types or value objects from Domain
    // Example: Guid OrderId, string Reason
) : IRequest<Result>;
// Use IRequest<Result<T>> if the command returns a value (e.g. a created resource ID)
```

---

### File 2 — Handler
**Path:** `Commands/{VerbNoun}/{VerbNoun}CommandHandler.cs`

```csharp
namespace {Project}.Application.Commands.{VerbNoun};

public sealed class {VerbNoun}CommandHandler : IRequestHandler<{VerbNoun}Command, Result>
{
    // TODO: inject required repository interfaces and domain services
    // Example: private readonly I{Noun}Repository _repository;

    public {VerbNoun}CommandHandler(/* TODO: inject dependencies */)
    {
        // TODO: assign injected dependencies
    }

    public async Task<Result> Handle(
        {VerbNoun}Command request,
        CancellationToken cancellationToken)
    {
        // TODO: implement handler logic
        // Pattern to follow:
        //   1. Load aggregate from repository
        //   2. Call domain method (business logic lives on the aggregate)
        //   3. Persist via repository
        //   4. SaveChangesAsync() — once, at the end
        //   5. Return Result.Success() or Result.Failure("reason")
        throw new NotImplementedException();
    }
}
```

---

### File 3 — Validator
**Path:** `Commands/{VerbNoun}/{VerbNoun}CommandValidator.cs`

```csharp
namespace {Project}.Application.Commands.{VerbNoun};

public sealed class {VerbNoun}CommandValidator : AbstractValidator<{VerbNoun}Command>
{
    public {VerbNoun}CommandValidator()
    {
        // TODO: add validation rules
        // Example:
        // RuleFor(x => x.OrderId).NotEmpty();
        // RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
```

---

### Step 3 — Remind the user what to do next

```
Next steps:
[ ] Fill in Command properties
[ ] Fill in Handler — load, act on domain, persist, return Result
[ ] Fill in Validator rules
[ ] Write unit tests for the handler — run /write-tests {VerbNoun}CommandHandler
[ ] Wire up the matching controller action — run /scaffold-endpoint if not done
```

## Rules
- All three files go in the same folder — one folder per command.
- Handlers must not contain domain logic. That belongs on the aggregate/domain entity.
- One `SaveChangesAsync()` per handler, at the very end — never inside a loop.
- Never return raw domain entities from a handler. Return a `Result`, `Result<T>`, or a DTO.
- The validator is always generated — even if it starts empty. It will be needed.
