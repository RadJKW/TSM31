# Developer Quick Start Guide

## Project Overview
This Blazor Hybrid application is designed for electrical transformer testing with full keyboard navigation.

## Running the Application

### Windows Forms Version (Recommended for Testing)
```powershell
cd MauiBlazor.WinForm
dotnet run
```

### Web Version (Recommended for Development)
```powershell
cd MauiBlazor.Web
dotnet run
```

### Hot Reload Development
For the best development experience with automatic reloading:
```powershell
cd MauiBlazor.Web
dotnet watch
```

**Note**: The splash screen initialization dialog will only show on fresh app start. During hot reload sessions (`dotnet watch`), the splash screen is automatically skipped to improve developer experience. This behavior is controlled by the `InitializationStateService` which persists initialization state across hot reloads and browser refreshes using localStorage.

## Key Architecture Decisions

### Why Blazor Hybrid?
- **Single Codebase**: Same UI runs on Windows, Web, and potentially mobile
- **Modern UI**: FluentUI components provide a polished, professional look
- **Maintainability**: C# and Razor instead of WinForms designer
- **Performance**: Native performance with Blazor WebView

### Component Organization

```
MauiBlazor.Shared/
├── Components/          # Reusable UI components
│   ├── DataEntryTab.razor
│   ├── HipotTab.razor
│   ├── ImpulseTab.razor
│   ├── InducedTab.razor
│   ├── MenuComponent.razor
│   ├── MenuButton.razor
│   ├── WelcomeScreen.razor
│   ├── SplashScreenDialog.razor
│   └── OperatorIdDialog.razor
├── Layout/              # Layout components
│   ├── TestStationLayout.razor  # Main application layout
│   ├── MainLayout.razor         # Legacy layout (not used)
│   └── NavMenu.razor            # Legacy nav (not used)
├── Models/              # Data models
│   └── TestModels.cs
├── Pages/               # Routable pages
│   ├── TestStationHome.razor    # Main page ("/")
│   ├── Home.razor               # Old demo page ("/old-home")
│   ├── Counter.razor            # Demo page
│   └── Weather.razor            # Demo page
├── Services/            # Application services
│   ├── KeyboardNavigationService.cs
│   ├── AppEnvironment.cs
│   ├── IFormFactor.cs
│   └── Contracts/
└── wwwroot/             # Static assets
```

## Keyboard Navigation Architecture

### Event Flow
1. User presses key in Windows Form
2. Key event captured by Blazor's `@onkeydown` in TestStationLayout
3. Event handler updates KeyboardNavigationService state
4. Service fires events to listening components
5. Components update their UI via StateHasChanged()

### Adding a New Tab

#### 1. Create the Component
```razor
@using MauiBlazor.Shared.Models

<FluentStack Orientation="Orientation.Vertical" Style="padding: 16px;">
    <FluentLabel Typo="Typography.Subject">My New Tab</FluentLabel>
    <!-- Your content here -->
</FluentStack>

@code {
    // Component logic
}
```

#### 2. Add to MainTab Enum
```csharp
public enum MainTab
{
    None,
    DataEntry,
    // ... existing tabs ...
    MyNewTab  // Add here
}
```

#### 3. Update MenuComponent
```razor
@if (KeyboardNav.CurrentMainTab == MainTab.None)
{
    <!-- Add to main menu -->
    <MenuButton KeyText="F11" Label="My New Tab" IsSelected="false" />
}
```

#### 4. Update TestStationLayout
```csharp
private void HandleKeyDown(KeyboardEventArgs e)
{
    // ... existing code ...
    switch (functionKey)
    {
        // ... existing cases ...
        case "F11":
            KeyboardNav.SetMainTab(MainTab.MyNewTab);
            break;
    }
}
```

#### 5. Add Route in TestStationHome
```razor
else if (KeyboardNav.CurrentMainTab == MainTab.MyNewTab)
{
    <MyNewTab />
}
```

## Working with FluentUI Components

### Common Patterns

#### Data Grid with Type Parameter
```razor
<FluentDataGrid Items="@myDataList.AsQueryable()" 
                TGridItem="MyDataType">
    <PropertyColumn Property="@(t => t.PropertyName)" Title="Display Name" />
</FluentDataGrid>
```

#### Dialog with Hidden Binding
```razor
<FluentDialog @bind-Hidden="@dialogHidden" Modal="true">
    <FluentStack Orientation="Orientation.Vertical">
        <!-- Dialog content -->
    </FluentStack>
</FluentDialog>

@code {
    private bool dialogHidden = true;
    
    public void Show() {
        dialogHidden = false;
        StateHasChanged();
    }
}
```

#### Responsive Layout
```razor
<FluentGrid Spacing="2">
    <FluentGridItem xs="12" sm="6" md="4">
        <FluentTextField Label="Field 1" />
    </FluentGridItem>
    <FluentGridItem xs="12" sm="6" md="4">
        <FluentTextField Label="Field 2" />
    </FluentGridItem>
</FluentGrid>
```

## Styling Guidelines

### Use FluentUI Tokens
```razor
<!-- Good -->
<div style="background: var(--neutral-layer-2); 
            color: var(--neutral-foreground-rest);
            border: 1px solid var(--neutral-stroke-rest);">
</div>

<!-- Avoid -->
<div style="background: #f0f0f0; color: #333; border: 1px solid #ccc;">
</div>
```

### Common Tokens
- **Backgrounds**: `--neutral-layer-1`, `--neutral-layer-2`, `--neutral-layer-3`
- **Text**: `--neutral-foreground-rest`, `--accent-fill-rest`
- **Borders**: `--neutral-stroke-rest`, `--neutral-stroke-divider-rest`
- **Interactive**: `--accent-fill-rest`, `--accent-fill-hover`, `--accent-fill-active`

## State Management

### Service Injection
```razor
@inject KeyboardNavigationService KeyboardNav

@code {
    protected override void OnInitialized()
    {
        KeyboardNav.OnMainTabChanged += HandleTabChange;
    }
    
    private void HandleTabChange(MainTab tab)
    {
        StateHasChanged();
    }
    
    public void Dispose()
    {
        KeyboardNav.OnMainTabChanged -= HandleTabChange;
    }
}
```

### Component Parameters
```razor
@code {
    [Parameter]
    public bool IsActive { get; set; }
    
    [Parameter]
    public EventCallback<string> OnValueChanged { get; set; }
    
    private async Task NotifyChange(string value)
    {
        await OnValueChanged.InvokeAsync(value);
    }
}
```

## Testing Integration Points

### Hardware Services (To Be Implemented)
```csharp
public interface IMeterService
{
    Task<MeterReading> GetReadingAsync();
    Task<bool> ConnectAsync();
}

public interface IGeneratorService
{
    Task SetVoltageAsync(decimal voltage);
    Task<GeneratorStatus> GetStatusAsync();
}
```

### Mock Data for Development
All tabs currently use mock data in their `@code` blocks:
```csharp
private List<HipotTestData> hipotTests = new()
{
    new HipotTestData { /* sample data */ }
};
```

Replace with actual service calls when hardware is available.

## Debugging Tips

### Enable Blazor Debug Mode
Add this to `MauiBlazor.WinForm/Program.cs`:
```csharp
services.AddBlazorWebViewDeveloperTools();
```

### Browser Dev Tools
- Press F12 in the running application
- Or right-click and select "Inspect"

### Logging
```csharp
@inject ILogger<MyComponent> Logger

@code {
    Logger.LogInformation("Component initialized");
    Logger.LogError(ex, "Error occurred");
}
```

## Common Issues

### Keyboard Events Not Working
- Ensure `KeyPreview = true` on the Form
- Check that the focus is on the WebView
- Verify `@onkeydown` is present in TestStationLayout

### Components Not Updating
- Call `StateHasChanged()` after state changes
- Ensure event handlers are properly subscribed/unsubscribed
- Check for proper disposal of event subscriptions

### FluentUI Components Not Rendering
- Verify `@using Microsoft.FluentUI.AspNetCore.Components` in _Imports.razor
- Check that `services.AddFluentUIComponents()` is called
- Ensure proper component syntax (no typos in property names)

### Need to See Splash Screen Again
If you want to force the splash screen to show (for testing or demonstration):
1. **In Browser**: Open DevTools (F12) → Application → Local Storage → Delete `testStation.initialized`
2. **In Code**: Inject `InitializationStateService` and call `await ClearInitializationAsync()`
3. **Complete Reset**: Close browser/app completely and restart

## Performance Considerations

### Virtual Scrolling for Large Lists
```razor
<FluentDataGrid Items="@largeDataSet" 
                Virtualize="true"
                ItemSize="50">
    <!-- columns -->
</FluentDataGrid>
```

### Debouncing User Input
```csharp
private System.Timers.Timer? debounceTimer;

private void OnInputChanged(string value)
{
    debounceTimer?.Stop();
    debounceTimer = new System.Timers.Timer(300);
    debounceTimer.Elapsed += (s, e) => ProcessInput(value);
    debounceTimer.AutoReset = false;
    debounceTimer.Start();
}
```

## Next Steps

1. **Integrate Hardware**: Implement meter and generator services
2. **Add Data Persistence**: Save test results to database
3. **Implement Auto Test**: Automated test sequence execution
4. **Add Reporting**: PDF generation for test results
5. **Enhance Safety**: Hardware interlock integration

## Resources

- [FluentUI Blazor Docs](https://www.fluentui-blazor.net/)
- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [.NET MAUI Blazor Hybrid](https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/)

## Support

For questions or issues, please contact the development team.
