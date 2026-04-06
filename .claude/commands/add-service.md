# /add-service

Scaffold a new service interface and skeleton implementation.
Works across all project types: Web API, Worker Service, WinForms, WPF.

## Usage
```
/add-service <ServiceName>
```

## Examples
```
/add-service JobSearch
/add-service SensorReading
/add-service NotificationDispatcher
```

---

## Instructions

You are scaffolding a service interface and its implementation.
Read the existing project structure before generating to confirm the correct folders.

---

### Step 1 - Identify folder targets

- Look for an `Interfaces/` folder - the interface goes here
- Look for a `Services/` folder - the implementation goes here
- If the project uses Clean Architecture:
  - Interface goes in `{Project}.Application/Interfaces/` or `{Project}.Domain/Interfaces/`
  - Implementation goes in `{Project}.Infrastructure/Services/`
- If unsure, ask before creating files in the wrong location

---

### File 1 - Interface
**Path:** `Interfaces/I{ServiceName}Service.cs`

```csharp
namespace {Project}.Interfaces;

public interface I{ServiceName}Service
{
    // TODO: define service contract methods
    // Example:
    // Task<IReadOnlyList<{ServiceName}>> GetAllAsync(CancellationToken cancellationToken = default);
    // Task<{ServiceName}?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Task AddAsync({ServiceName} item, CancellationToken cancellationToken = default);
    // Task UpdateAsync({ServiceName} item, CancellationToken cancellationToken = default);
    // Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

---

### File 2 - Implementation
**Path:** `Services/{ServiceName}Service.cs`

```csharp
namespace {Project}.Services;

public sealed class {ServiceName}Service : I{ServiceName}Service
{
    private readonly ILogger<{ServiceName}Service> _logger;

    // TODO: inject additional dependencies (repositories, DbContext, HttpClient, etc.)

    public {ServiceName}Service(ILogger<{ServiceName}Service> logger)
    {
        _logger = logger;
    }

    // TODO: implement interface methods
    // Follow these rules:
    //   - All I/O methods must be async
    //   - Always accept and pass through CancellationToken
    //   - Log at the appropriate level (Information for normal ops, Warning for expected failures)
    //   - Never swallow exceptions silently
}
```

---

### Step 3 - Remind the user what to do next

```
Next steps:
[ ] Define the interface methods in I{ServiceName}Service.cs
[ ] Implement the methods in {ServiceName}Service.cs
[ ] Register in DI - run /wire-di {ServiceName}Service
[ ] Write tests - run /write-tests {ServiceName}Service
```

## Rules
- Always define the interface before the implementation - never skip the interface.
- Never inject concrete types - only interfaces.
- Always include ILogger<T> in the constructor - even if not immediately used.
- Service methods that do I/O must be async with CancellationToken.
- Do not add business logic that belongs in a domain entity or handler.
