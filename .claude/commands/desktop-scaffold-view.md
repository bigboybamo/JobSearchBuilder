# /scaffold-view

Scaffold a new WPF View (XAML + code-behind) and its matching ViewModel.

## Usage
```
/scaffold-view <ViewName> [purpose]
```

## Examples
```
/scaffold-view CustomerList
/scaffold-view AddJobApplication entry form
/scaffold-view SensorDashboard display dashboard
```

---

## Instructions

You are scaffolding a WPF View and ViewModel pair.
Read the existing project structure first to confirm:
- Which MVVM framework is in use (CommunityToolkit.Mvvm, Prism, ReactiveUI, plain INotifyPropertyChanged)
- Where Views and ViewModels live (`Views/`, `ViewModels/`)
- How ViewModels are currently injected into Views (constructor, DataContext in code-behind, ViewModelLocator)
- The namespace pattern already in use

---

### File 1 - View XAML
**Path:** `Views/{ViewName}View.xaml`

```xml
<Window x:Class="{Project}.Views.{ViewName}View"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:vm="clr-namespace:{Project}.ViewModels"
        mc:Ignorable="d"
        d:DataContext="{d:DesignInstance Type=vm:{ViewName}ViewModel}"
        Title="{ViewName}" Height="450" Width="800">

    <Grid>
        <!-- TODO: add UI controls and bind to ViewModel properties -->
        <!-- Example:
        <StackPanel Margin="16">
            <TextBox Text="{Binding SearchTerm, UpdateSourceTrigger=PropertyChanged}" />
            <Button Content="Search" Command="{Binding SearchCommand}" />
            <ListView ItemsSource="{Binding Results}" />
        </StackPanel>
        -->
    </Grid>
</Window>
```

---

### File 2 - View code-behind
**Path:** `Views/{ViewName}View.xaml.cs`

```csharp
namespace {Project}.Views;

public partial class {ViewName}View : Window
{
    public {ViewName}View({ViewName}ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

---

### File 3 - ViewModel
**Path:** `ViewModels/{ViewName}ViewModel.cs`

Generate using CommunityToolkit.Mvvm if present, otherwise plain INotifyPropertyChanged.

**With CommunityToolkit.Mvvm:**
```csharp
namespace {Project}.ViewModels;

public partial class {ViewName}ViewModel : ObservableObject
{
    private readonly I{RelatedService}Service _service;
    private readonly ILogger<{ViewName}ViewModel> _logger;

    // TODO: add observable properties
    // Example:
    // [ObservableProperty]
    // private string _searchTerm = string.Empty;
    //
    // [ObservableProperty]
    // private ObservableCollection<{Item}> _results = new();
    //
    // [ObservableProperty]
    // private bool _isBusy;

    public {ViewName}ViewModel(
        I{RelatedService}Service service,
        ILogger<{ViewName}ViewModel> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // TODO: add commands
    // Example:
    // [RelayCommand]
    // private async Task SearchAsync()
    // {
    //     try
    //     {
    //         IsBusy = true;
    //         Results = new ObservableCollection<{Item}>(await _service.SearchAsync(SearchTerm));
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Search failed");
    //     }
    //     finally
    //     {
    //         IsBusy = false;
    //     }
    // }
}
```

**Without CommunityToolkit (plain INPC):**
```csharp
namespace {Project}.ViewModels;

public class {ViewName}ViewModel : INotifyPropertyChanged
{
    private readonly I{RelatedService}Service _service;
    private readonly ILogger<{ViewName}ViewModel> _logger;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // TODO: add properties with OnPropertyChanged in setter
    // TODO: add ICommand properties

    public {ViewName}ViewModel(
        I{RelatedService}Service service,
        ILogger<{ViewName}ViewModel> logger)
    {
        _service = service;
        _logger  = logger;
    }
}
```

---

### Step 4 - Remind the user what to do next

```
Next steps:
[ ] Add controls to {ViewName}View.xaml and bind to ViewModel properties
[ ] Implement commands and properties in {ViewName}ViewModel
[ ] Register both in DI - run /wire-di {ViewName}View and /wire-di {ViewName}ViewModel
[ ] Write tests for the ViewModel - run /write-tests {ViewName}ViewModel
```

## Rules
- No business logic in code-behind. Code-behind only sets DataContext - nothing else.
- No business logic in the ViewModel either - delegate to services.
- All async ViewModel commands must handle exceptions internally and update UI state (IsBusy etc).
- Never use async void in ViewModels - RelayCommand handles this correctly.
- Always inject services via constructor - never resolve from a service locator.
- Use d:DataContext for design-time data binding support - always include it.
