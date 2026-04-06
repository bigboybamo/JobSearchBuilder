# /wire-di

Generate the DI registration snippet for a new service, form, view, or ViewModel.
Works across all project types: Web API, Worker Service, WinForms, WPF.

## Usage
```
/wire-di <TypeName>
```

## Examples
```
/wire-di JobSearchService
/wire-di SensorReadingService
/wire-di MainViewModel
/wire-di CustomerForm
```

---

## Instructions

You are generating a DI registration snippet.
Read `Program.cs` or the DI registration file before generating to confirm the correct location and pattern already in use.

---

### Step 1 - Identify the registration file

Look for DI registrations in this order:
1. `Program.cs` - most common in .NET 6+ minimal hosting
2. `Startup.cs` - older ASP.NET Core projects
3. An extension method file like `ServiceCollectionExtensions.cs` or `ApplicationServices.cs`
4. For WinForms/WPF - look for a `Bootstrapper.cs`, `App.xaml.cs`, or a `Host` builder setup

Read the existing registrations to confirm the lifetime pattern already in use before suggesting a lifetime.

---

### Step 2 - Generate the registration snippet

**Standard service registration:**
```csharp
// In Program.cs or the relevant extension method
// TODO: confirm correct lifetime - see guidance below
builder.Services.AddScoped<I{TypeName}, {TypeName}>();
```

**Lifetime guidance:**
| Lifetime | Use when |
|---|---|
| `AddScoped` | Default for most services - one instance per request (API) or per scope (desktop) |
| `AddSingleton` | Shared state across the entire app lifetime - use sparingly, must be thread-safe |
| `AddTransient` | Lightweight, stateless services - new instance every time it is resolved |

**WinForms registration (if applicable):**
```csharp
// Forms should be registered as Transient - a new instance each time they are opened
services.AddTransient<{FormName}>();
services.AddScoped<I{ServiceName}Service, {ServiceName}Service>();
```

**WPF registration (if applicable):**
```csharp
// ViewModels are typically Transient unless they hold shared state
services.AddTransient<{ViewModelName}>();
services.AddScoped<I{ServiceName}Service, {ServiceName}Service>();
```

---

### Step 3 - Show where to place it

Point to the exact file and approximate line where the snippet should be added:
- Place it near other registrations of the same type (group services together)
- If an extension method exists for the layer (e.g. `services.AddApplication()`), add it inside that method
- Never add infrastructure registrations directly in `Program.cs` if an extension method exists

---

### Step 4 - Remind the user what to do next

```
Next steps:
[ ] Add the registration snippet to {RegistrationFile}
[ ] Verify the lifetime is correct for this service's usage pattern
[ ] Confirm the service resolves correctly by running the project
```

## Rules
- Always read the existing DI registrations before suggesting a lifetime - match the project's existing pattern.
- Never register a Singleton that depends on a Scoped service - flag this as a lifetime violation if found.
- Always register by interface, not concrete type - `AddScoped<IMyService, MyService>()` not `AddScoped<MyService>()`.
- For WinForms Forms and WPF Views, always use Transient unless there is a specific reason for Singleton.
- Do not generate the full Program.cs - only the snippet that needs to be added.
