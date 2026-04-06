# /scaffold-form

Scaffold a new WinForms Form with code-behind and a wired-up service call.

## Usage
```
/scaffold-form <FormName> [purpose]
```

## Examples
```
/scaffold-form CustomerList
/scaffold-form AddJobApplication entry form
/scaffold-form SensorDashboard display dashboard
```

---

## Instructions

You are scaffolding a WinForms Form. Read the existing project structure first to confirm:
- Which folder Forms live in (common: `Forms/`, `Views/`, or project root)
- The base Form class or template already in use
- How services are currently injected into Forms
- The DI registration pattern (constructor injection via hosted service, or manual resolve)

---

### File 1 - Form code-behind
**Path:** `Forms/{FormName}.cs` (or matching existing Forms folder)

```csharp
namespace {Project}.Forms;

public partial class {FormName} : Form
{
    private readonly I{RelatedService}Service _service;
    private readonly ILogger<{FormName}> _logger;

    public {FormName}(
        I{RelatedService}Service service,
        ILogger<{FormName}> logger)
    {
        InitializeComponent();
        _service = service;
        _logger  = logger;
    }

    private async void {FormName}_Load(object sender, EventArgs e)
    {
        // TODO: load initial data when form opens
        // Example:
        // await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // TODO: call service and bind results to controls
            // Example:
            // var items = await _service.GetAllAsync();
            // dataGridView1.DataSource = items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load data in {FormName}");
            MessageBox.Show(
                "Failed to load data. Please try again.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // TODO: add event handlers for buttons, grids, and other controls below
    // Keep event handlers thin - delegate work to private async Task methods
    // Never put business logic directly in event handlers
}
```

---

### File 2 - Designer file note

Do NOT generate the `.Designer.cs` file - Visual Studio generates this automatically
when you create the Form through the IDE.

Instruct the user:
```
Create the Form in Visual Studio first (Add -> New Item -> Windows Form),
then replace the generated {FormName}.cs content with the scaffold above.
The {FormName}.Designer.cs file is managed by the designer - do not edit it manually.
```

---

### Step 3 - Remind the user what to do next

```
Next steps:
[ ] Create {FormName} in Visual Studio via Add -> New Item -> Windows Form
[ ] Replace {FormName}.cs with the scaffolded code above
[ ] Add controls to the form using the Visual Studio designer
[ ] Register the form in DI - run /wire-di {FormName}
[ ] Implement LoadDataAsync and event handlers
[ ] Write tests for the service methods called by this form - run /write-tests {RelatedService}Service
```

## Rules
- Never put business logic in event handlers - always delegate to a private async Task method.
- Always inject services via constructor - never use ServiceLocator or static access.
- All async operations must be wrapped in try/catch with user-friendly error messages.
- Never use async void except for event handlers - and even then, delegate immediately to async Task.
- Do not generate the Designer.cs file - Visual Studio owns that file.
- Always log exceptions before showing a MessageBox - never swallow silently.
