# /scaffold-endpoint

Scaffold a new Controller-based API endpoint following project conventions.

## Usage
```
/scaffold-endpoint <ResourceName> <HttpMethod> [route]
```

## Examples
```
/scaffold-endpoint Order GET api/orders/{id}
/scaffold-endpoint Product POST api/products
/scaffold-endpoint Invoice DELETE api/invoices/{id}
```

## Instructions

You are scaffolding a Controller-based API endpoint. Follow these steps in order:

### Step 1 — Identify where files go
- Controller → `src/{Project}.API/Controllers/{ResourceName}Controller.cs`
- If the controller already exists, add the action method to it instead of creating a new file.

### Step 2 — Generate the Controller

```csharp
namespace {Project}.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class {ResourceName}Controller : ControllerBase
{
    private readonly IMediator _mediator;

    public {ResourceName}Controller(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Http{Method}("{route}")]
    [ProducesResponseType(typeof({ResponseType}), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> {ActionName}(
        // TODO: add parameters — route params, [FromBody], [FromQuery] as appropriate
        CancellationToken cancellationToken)
    {
        // TODO: construct and send the appropriate Command or Query via _mediator
        // Example: var result = await _mediator.Send(new Get{ResourceName}Query(...), cancellationToken);
        // TODO: map result to IActionResult — return Ok(result.Value) or NotFound() / BadRequest()
        throw new NotImplementedException();
    }
}
```

### Step 3 — Remind the user what to do next
After generating the controller, output this checklist:

```
Next steps:
[ ] Create the matching Command or Query — run /add-command or /add-query
[ ] Register any new services in DI if needed
[ ] Add ProducesResponseType attributes for all possible outcomes
[ ] Add XML doc comments if this API is public-facing
[ ] Write integration tests for this endpoint
```

## Rules
- Never put business logic in the controller. The controller only sends to MediatR and maps the result.
- Always inject `IMediator` — never inject repositories or services directly into controllers.
- Always include `CancellationToken` as the last parameter.
- Use `TypedResults` if the project has already adopted Minimal API style — otherwise stick to `IActionResult`.
- Do not generate the request/response body classes in this command — that is done by `/add-command` or `/add-query`.
