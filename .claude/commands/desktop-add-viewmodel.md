# /add-viewmodel

Scaffold a ViewModel skeleton for an existing WPF View.
Use this when the View already exists and you need to add or replace its ViewModel.

## Usage
```
/add-viewmodel <ViewModelName>
```

## Examples
```
/add-viewmodel CustomerList
/add-viewmodel JobApplicationDetail
/add-viewmodel SettingsPanel
```

---

## Instructions

You are scaffolding a ViewModel for an existing View.
Read the following before generating:
1. The matching View XAML - identify what bindings are already declared (`{Binding PropertyName}`, `Command="{Binding CommandName}"`)
2. The MVVM framework in use (CommunityToolkit.Mvvm, Prism, ReactiveUI, plain INPC)
3. The base class used by existing ViewModels
4. Which services the View will need based on what it displays

Generate properties and commands to match the bindings already declared in the XAML.
Do not invent bindings that are not already in the View.

---

### File - ViewModel
**Path:** `ViewModels/{ViewModelName}ViewModel.cs`

**With CommunityToolkit.Mvvm (preferred):**
```csharp
namespace {Project}.ViewModels;

public partial class {ViewModelName}ViewModel : ObservableObject
{
    private readonly I{RelatedService}Service _service;
    private readonly ILogger<{ViewModelName}ViewModel> _logger;

    // Generated from XAML bindings found in {ViewModelName}View.xaml:
    // TODO: add [ObservableProperty] for each {Binding PropertyName} found in XAML
    // TODO: add [RelayCommand] for each Command="{Binding CommandName}" found in XAML

    // Example - replace with actual bindings from the View:
    // [ObservableProperty]
    // [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    // private string _title = string.Empty;
    //
    // [ObservableProperty]
    // private bool _isBusy;
    //
    // [ObservableProperty]
    // private ObservableCollection<ItemViewModel> _items = new();

    public {ViewModelName}ViewModel(
        I{RelatedService}Service service,
        ILogger<{ViewModelName}ViewModel> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // TODO: add [RelayCommand] methods matching Command bindings in XAML
    // Example:
    // [RelayCommand]
    // private async Task LoadAsync()
    // {
    //     try
    //     {
    //         IsBusy = true;
    //         var data = await _service.GetAllAsync();
    //         Items = new ObservableCollection<ItemViewModel>(data.Select(x => new ItemViewModel(x)));
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Failed to load {ViewModelName}");
    //     }
    //     finally
    //     {
    //         IsBusy = false;
    //     }
    // }
    //
    // [RelayCommand(CanExecute = nameof(CanSave))]
    // private async Task SaveAsync() { }
    //
    // private bool CanSave() => !string.IsNullOrWhiteSpace(Title) && !IsBusy;
}
```

**Without CommunityToolkit (plain INPC):**
```csharp
namespace {Project}.ViewModels;

public class {ViewModelName}ViewModel : INotifyPropertyChanged
{
    private readonly I{RelatedService}Service _service;
    private readonly ILogger<{ViewModelName}ViewModel> _logger;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // TODO: add backing fields and properties for each Binding in XAML
    // TODO: add ICommand properties for each Command binding in XAML

    public {ViewModelName}ViewModel(
        I{RelatedService}Service service,
        ILogger<{ViewModelName}ViewModel> logger)
    {
        _service = service;
        _logger  = logger;
    }
}
```

---

### Step 2 - Wire up DataContext

If the View's code-behind does not already set the DataContext, generate the update:

```csharp
// In {ViewModelName}View.xaml.cs
public {ViewModelName}View({ViewModelName}ViewModel viewModel)
{
    InitializeComponent();
    DataContext = viewModel;
}
```

---

### Step 3 - Remind the user what to do next

```
Next steps:
[ ] Fill in [ObservableProperty] fields matching XAML bindings
[ ] Fill in [RelayCommand] methods matching XAML command bindings
[ ] Register in DI - run /wire-di {ViewModelName}ViewModel
[ ] Confirm DataContext is set in the View code-behind
[ ] Write tests - run /write-tests {ViewModelName}ViewModel
```

## Rules
- Always read the XAML before generating - match properties and commands to actual bindings.
- Never add properties or commands not referenced in the XAML - keep the ViewModel lean.
- Never put business logic in the ViewModel - delegate to services.
- Always handle exceptions inside commands - never let them bubble up unhandled to the UI thread.
- Always include an IsBusy property for any ViewModel with async commands - bind it to a loading indicator.
- Use [NotifyCanExecuteChangedFor] to automatically re-evaluate CanExecute when related properties change.
