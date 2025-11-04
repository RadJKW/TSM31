# Initialization State Management

## Overview
The application implements smart initialization state management to improve developer experience during hot reload sessions while maintaining the professional splash screen for production deployments.

## Behavior

### Fresh Application Start
When the application is started for the first time (or after clearing state):
1. Splash screen displays with "Large Pole Dielectric Console"
2. Initialization sequence runs showing:
   - Checking I/O Control Panel
   - Initializing Power Generator
   - Connecting to Meter
   - Verifying Safety Systems
   - Loading Test Configurations
3. Each step shows progress (Pending → In Progress → OK/Warning)
4. "System Ready" message appears with "Continue" button
5. State is persisted to localStorage and in-memory

### Hot Reload (dotnet watch)
During active development with `dotnet watch`:
1. Code changes trigger hot reload
2. Application state is preserved in memory
3. Splash screen is **automatically skipped**
4. User lands directly on the last active page
5. Development continues seamlessly

### Browser Refresh
When user manually refreshes the browser:
1. Application checks localStorage for initialization state
2. Finds `testStation.initialized = true`
3. Splash screen is **skipped**
4. User sees the application immediately

### Force Splash Screen
To see the splash screen again (for testing/demo):

**Option 1: Browser DevTools**
1. Press F12 to open DevTools
2. Go to Application tab
3. Select Local Storage
4. Find and delete `testStation.initialized` key
5. Refresh the page

**Option 2: Code**
```csharp
@inject InitializationStateService InitStateService

private async Task ResetInitialization()
{
    await InitStateService.ClearInitializationAsync();
    // Refresh or navigate to trigger splash
}
```

**Option 3: Complete Reset**
- Close browser completely
- Clear browser cache
- Restart application

## Implementation Details

### InitializationStateService
Located: `MauiBlazor.Shared/Services/InitializationStateService.cs`

**Purpose**: Manages initialization state across hot reloads and browser refreshes.

**Key Methods**:
- `HasBeenInitializedAsync()`: Checks if app has been initialized
- `MarkAsInitializedAsync()`: Marks app as initialized (persists to localStorage)
- `ClearInitializationAsync()`: Resets initialization state

**Storage Strategy**:
1. **In-Memory Flag** (`_hasInitializedInSession`): 
   - Static variable survives hot reloads
   - Fastest check, no async overhead
   - Reset only on full app restart

2. **localStorage** (`testStation.initialized`):
   - Persists across browser refreshes
   - Survives full page reloads
   - Cleared only manually or on browser cache clear

### TestStationLayout Integration
Located: `MauiBlazor.Shared/Layout/TestStationLayout.razor`

**Modified Behavior**:
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await JS.InvokeVoidAsync("eval", "document.querySelector('.test-station-layout')?.focus()");

        // Show splash screen only on fresh startup, skip on hot reloads and refreshes
        var hasBeenInitialized = await InitStateService.HasBeenInitializedAsync();
        if (!hasBeenInitialized)
        {
            splashDialog?.Show();
        }
    }
}
```

### SplashScreenDialog Integration
Located: `MauiBlazor.Shared/Components/SplashScreenDialog.razor`

**Mark as Initialized**:
```csharp
private async Task Close()
{
    dialogHidden = true;
    StateHasChanged();
    
    // Mark as initialized so splash won't show on hot reloads or refreshes
    await InitStateService.MarkAsInitializedAsync();
}
```

### Service Registration
Located: `MauiBlazor.Shared/Services/ServiceCollectionExtensions.cs`

```csharp
// Add initialization state service as a singleton
// This tracks whether the app has been initialized to skip splash screen on hot reloads
services.AddSingleton<InitializationStateService>();
```

Registered as **Singleton** because:
- State must persist across component lifecycle
- Single source of truth for initialization state
- Static field maintains state across hot reloads

## Benefits

### For Developers
✅ **Fast Hot Reload**: No splash screen delay during development
✅ **Preserved Context**: Return to same page after code changes
✅ **Efficient Workflow**: Immediate feedback on changes
✅ **Easy Testing**: Can force splash screen when needed

### For Production
✅ **Professional First Impression**: Full splash screen on startup
✅ **System Validation**: Initialization checks visible to operators
✅ **Status Feedback**: Clear indication of system readiness
✅ **Consistent Experience**: Always shows on fresh application start

### For QA/Demo
✅ **Easy Reset**: Multiple ways to force splash screen
✅ **Testable**: Can verify both with/without splash
✅ **Predictable**: Clear rules for when splash appears

## Technical Considerations

### Why Static Field?
The static field `_hasInitializedInSession` is critical for hot reload support:
- **Non-static**: Would reset to false on every hot reload
- **Static**: Survives hot reload, preserves state
- **Singleton Service**: Ensures single instance manages the static state

### Why localStorage?
localStorage provides browser-level persistence:
- **Survives Page Refresh**: State maintained across F5 refresh
- **Per-Domain**: Each deployment has independent state
- **Easy to Clear**: DevTools provide simple reset mechanism
- **No Backend Required**: Client-side only solution

### Error Handling
The service gracefully handles JavaScript disconnection:
- **Try-Catch Blocks**: Catches JSDisconnectedException and InvalidOperationException
- **Fallback to Memory**: If localStorage fails, in-memory flag still works
- **No Errors**: User never sees exceptions from state management

### Cross-Platform Compatibility
- **Web**: Uses localStorage (primary use case)
- **WinForms WebView**: localStorage works in WebView2
- **MAUI**: Would use Preferences API instead (future enhancement)

## Future Enhancements

### Potential Improvements
1. **Configurable Behavior**: App setting to always show/skip splash
2. **Session Timeout**: Auto-clear state after X hours
3. **Development Mode Detection**: Auto-skip in DEBUG builds
4. **Preference API Integration**: For MAUI platforms
5. **Animation Skip**: Fast-forward initialization animation on subsequent runs

### MAUI Platform Support
For true cross-platform MAUI apps, replace localStorage with:
```csharp
// Using .NET MAUI Preferences API
await Preferences.SetAsync("testStation.initialized", "true");
var initialized = await Preferences.GetAsync("testStation.initialized", "false");
```

## Testing Scenarios

### Test Case 1: Fresh Start
1. Clear localStorage
2. Start application
3. **Expected**: Splash screen appears with full initialization sequence

### Test Case 2: Hot Reload
1. Run `dotnet watch`
2. Modify a .razor file
3. Save changes
4. **Expected**: Hot reload occurs, no splash screen, immediate UI update

### Test Case 3: Browser Refresh
1. Application running normally
2. Press F5 to refresh
3. **Expected**: Page reloads, no splash screen

### Test Case 4: Force Splash
1. Open DevTools → Application → Local Storage
2. Delete `testStation.initialized`
3. Refresh page
4. **Expected**: Splash screen appears

### Test Case 5: New Browser Session
1. Close all browser windows
2. Reopen browser
3. Navigate to application
4. **Expected**: Splash screen appears (localStorage cleared on some browsers)

## Documentation
- User Guide: See [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md#hot-reload-development)
- Architecture: Initialization flow in [ARCHITECTURE.md](ARCHITECTURE.md)
- README: Quick reference in [README.md](README.md#notes)

## Summary
The `InitializationStateService` provides an elegant solution to the "splash screen during development" problem by combining in-memory state (for hot reload) with localStorage persistence (for browser refresh), resulting in a professional production experience and efficient development workflow.
