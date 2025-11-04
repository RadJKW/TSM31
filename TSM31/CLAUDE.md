# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TSM31 is a keyboard-navigatable Blazor Hybrid application for electrical transformer dielectric testing. It runs as a Windows Forms host with a full-screen interface (1920x1080) optimized for industrial test stations. The app is a C# port of a legacy VB.NET WinForms application.

**Target Framework:** .NET 9.0

## Project Structure

- **TSM31.Core** - Razor class library containing all UI components, services, and models
- **TSM31.WinForm** - Windows Forms host application (primary deployment target)
- **TSM31.Web** - ASP.NET Core web host (optional/development)
- **TSM31.TestData** - Data layer with Entity Framework Core DbContext, entities, and models
- **TSM31.WPF** - WPF host (currently unused)

**Important:** All UI development happens in `TSM31.Core`. The host projects only bootstrap the Blazor runtime.

## Development Environment

**Environment Detection:**

This project is developed on Windows but may be accessed through WSL or directly on Windows. To determine which environment you're running in:

- **WSL Environment:** Working directory starts with `/mnt/` (e.g., `/mnt/d/TSM31/TSM31`)
- **Windows Environment:** Working directory uses Windows paths (e.g., `D:\TSM31\TSM31` or `C:\Users\...`)

**Command Execution by Environment:**

### If Running in WSL (path starts with `/mnt/`):

When executing commands like `dotnet`, `git`, or other Windows-specific tools from WSL:

- **DO NOT** run commands directly in the WSL shell
- **DO** use `cmd.exe /c` to invoke Windows Command Prompt
- All .NET SDK commands must be executed on the Windows side, not in WSL

**Correct command pattern from WSL:**

```bash
# From WSL, invoke Windows cmd to run dotnet commands
cmd.exe /c "dotnet build"
cmd.exe /c "dotnet run"
cmd.exe /c "git status"
```

**Why this matters:**

- The .NET SDK is installed on Windows, not in WSL
- File paths reference the Windows filesystem (D:\TSM31\TSM31)
- SQL Server connection requires Windows authentication
- Windows Forms and WPF hosts require Windows runtime

### If Running on Windows (native Windows path):

When executing commands directly on Windows, use standard command syntax without the `cmd.exe /c` wrapper:

```bash
# Direct Windows execution
dotnet build
dotnet run
git status
```

**Important:** Always check your current working directory to determine which command pattern to use. The `/mnt/` prefix is the definitive indicator of WSL environment.

## Common Commands

### Build & Run

**From WSL (using cmd.exe):**

```bash
# Build entire solution
cmd.exe /c "dotnet build"

# Build specific configuration
cmd.exe /c "dotnet build --configuration Release"

# Run Windows Forms application
cmd.exe /c "cd src/TSM31.WinForm && dotnet run"

# Run Web version
cmd.exe /c "cd src/TSM31.Web && dotnet run"
```

**Directly from Windows Command Prompt:**

```cmd
# Build entire solution
dotnet build

# Build specific configuration
dotnet build --configuration Release

# Run Windows Forms application
cd src\TSM31.WinForm
dotnet run

# Run Web version
cd src\TSM31.Web
dotnet run
```

### Database

The application uses SQL Server with Entity Framework Core. Connection string is hardcoded in `TestDataDbContext.cs`:

```
Server=RAD-SQL;Database=TestData;Integrated Security=True;...
```

**Note:** There are currently no migrations. The DbContext was reverse-engineered from an existing database.

## Architecture

### Service Layer (Singleton Services)

All core services are registered in `ServiceCollectionExtensions.cs`:

- **KeyboardNavigationService** - Centralized keyboard navigation state management

  - Tracks current main tab (`MainTab` enum) and sub-tabs (string)
  - Publishes events: `OnKeyPressed`, `OnMainTabChanged`, `OnSubTabChanged`, `OnStateChanged`
  - Manages test power state and download availability flags
- **UnitDataService** - Manages transformer unit data lifecycle

  - Downloads unit data from SQL Server (`DownloadUnitAsync`)
  - Parses test parameters from `Params` table into `UnitData` model
  - Maintains `CurrentUnit` state
  - Event: `OnUnitDataChanged`
- **InitializationStateService** - Manages app initialization state using localStorage

  - Prevents splash screen on hot reload during development
  - Persists initialization state across page refreshes
- **MenuDisplayService** - Dynamically generates menu content based on current context
- **AppStateService** - Global application state container
- **TestConfigurationService** - Test configuration and parameter management
- **DielectricTestManager** - Orchestrates dielectric test execution

### Navigation Flow

Function keys drive navigation through a state machine:

```
ESC → Return to menu/previous screen
F1 → Data Entry
F2 → First Induced Test
F3 → Hipot Test (may show submenu)
F4 → Impulse Test (may show submenu)
F5 → Second Induced Test
F6 → Auto Test (planned)
F7 → Enter Operator ID
F8 → Upload Data
F9 → Re-Print Fail Tag
F10 → Next Test (cycle through tests)
```

**Key Pattern:**

1. User presses function key (or clicks button)
2. `TestStationLayout.razor` captures `@onkeydown` event
3. `KeyboardNavigationService.HandleKeyPress()` is called
4. Service raises `OnMainTabChanged` or `OnSubTabChanged` event
5. Components subscribed to events update their state
6. `StateHasChanged()` triggers re-render

### Data Models (TSM31.TestData)

**UnitData** - Root model for a transformer unit

- Collections: `Ratings`, `Hipot`, `Induced`, `Impulse` (one per test number)
- Each test has corresponding status objects (`TestStatus` with `TestStatusType` enum)

**TestStatusType Enum:**

- `NotRequired` - Test not needed
- `Required` - Must be performed
- `Passed` - Successfully completed
- `Failed` - Failed test
- `Aborted` - Started but not completed (retryable)

**Status Transitions:**

- `Required` → `Passed` (test completes successfully)
- `Required` → `Failed` (F5 key or test failure)
- `Required` → `Aborted` (timer abort, power loss, cancel)
- `Aborted` → `Passed` (retry successful)
- `Aborted` → `Failed` (F5 key)

### Component Hierarchy

```
Routes.razor
└─ TestStationLayout.razor (MainLayout)
   ├─ TransformerDataBar (Header)
   ├─ MenuComponent (Left sidebar)
   │  └─ MenuButton (multiple instances)
   ├─ Test Power Control (built-in)
   ├─ TestStationHome.razor (Body, route "/")
   │  ├─ WelcomeScreen
   │  ├─ DataEntryTab
   │  ├─ HipotTab
   │  ├─ ImpulseTab
   │  └─ InducedTab
   ├─ DownloadDialog (on-demand)
   ├─ SplashScreenDialog (startup)
   └─ OperatorIdDialog (on-demand)
```

## UI Framework

**FluentUI + Tailwind CSS:**

- Primary: Microsoft FluentUI Blazor components (`Microsoft.FluentUI.AspNetCore.Components`)
- Utility: TailwindCSS v4 for spacing, layout, and custom styles
- Minimize conflicts by using FluentUI for interactive components, Tailwind for layout

**Design Principles:**

- Compact, high-density layouts for industrial screens
- All functionality keyboard-accessible
- Visual feedback for states (colors: Green=Passed, Red=Failed, Yellow=Aborted, LightBlue=current test)

## Download Flow (Critical Architecture)

Understanding unit download is essential for working with this codebase:

1. **User Action:** F8 on Data Entry tab or Download button
2. **Service Call:** `UnitDataService.DownloadUnitAsync(serialNumber)`
3. **SQL Lookup:**
   - Query `Xref` table by serial number → get catalog number and work order
   - Query `Params` table by work order + catalog number → get test parameters (multiple rows, one per test)
4. **Parsing:** Each `Params` row is parsed into:
   - `Ratings` (voltages, currents, BILs)
   - `HipotData` (status, limits, set conditions)
   - `InducedData` (status, time requirements, watt limits)
   - `ImpulseData` (status per bushing: H1, H2, H3, X1, X2)
5. **State Update:**
   - `CurrentUnit.IsDownloaded = true`
   - `OnUnitDataChanged` event raised
   - UI components re-render to show downloaded data

**Important Files:**

- `UnitDataService.cs:22-82` - Download orchestration
- `UnitDataService.cs:84-191` - Parameter parsing logic
- `TestDataDbContext.cs` - EF Core context with `Params` and `Xrefs` DbSets

## JavaScript Interop

**keyboard-handler.js** - Custom keyboard event handling

- Registers global keydown listeners
- Prevents default browser behavior for function keys
- Forwards events to Blazor components via JSInterop

**Usage in Blazor:**

```csharp
@inject IJSRuntime JS
await JS.InvokeVoidAsync("keyboardHandler.initialize");
```

## Key Patterns

### Event-Driven State Updates

Services use C# events for loose coupling:

```csharp
// Service
public event Action<MainTab>? OnMainTabChanged;

// Component
protected override void OnInitialized()
{
    NavService.OnMainTabChanged += HandleTabChanged;
}

public void Dispose()
{
    NavService.OnMainTabChanged -= HandleTabChanged;
}
```

### Test Execution Pattern

1. **Mode Selection:** Manual, Set-and-Record, or Auto
2. **Power On:** F1 toggles test power
3. **Set Condition:** Adjust voltage/current to test tolerance
4. **Timer Start:** F3 starts test timer
5. **Completion:** Auto-record on success or abort on violation
6. **Status Update:** Mutate `TestStatus` object in collection
7. **UI Refresh:** Component reloads recorded data list

### Dialog Pattern

All dialogs use FluentUI's `FluentDialog`:

```razor
<FluentDialog @bind-Hidden="_isHidden" Modal="true">
    <!-- Dialog content -->
</FluentDialog>

@code {
    private bool _isHidden = true;

    public void Show() => _isHidden = false;
}
```

## File Naming Conventions

- **Razor Components:** PascalCase, `.razor` extension (e.g., `DataEntryTab.razor`)
- **Services:** PascalCase, `Service` suffix (e.g., `KeyboardNavigationService.cs`)
- **Models:** PascalCase, descriptive names (e.g., `HipotData.cs`, `TestStatus.cs`)
- **Layouts:** Suffix with `Layout` (e.g., `TestStationLayout.razor`)

## Test Tab Implementation Pattern

When working on test tabs (Hipot, Induced, Impulse), follow this structure:

1. **Parameter List:** FluentDataGrid showing test parameters
2. **Recorded Data List:** FluentDataGrid showing completed test results
3. **Control Panel:** Buttons for F-key actions (Power, Timer, Mode, etc.)
4. **Meter Displays:** Real-time readings (voltage, current, watts, time)
5. **Status Bar:** Test status, mode, power state

**Color Coding:**

- Voltage labels turn Yellow when in test tolerance
- Recorded status cells: Green (Passed), Red (Failed), Yellow (Aborted)
- Current test row: LightBlue background

## Common Development Tasks

### Adding a New Service

1. Create service class in `TSM31.Core/Services/`
2. Register in `ServiceCollectionExtensions.cs`:
   ```csharp
   services.AddSingleton<YourService>();
   ```
3. Inject into components:
   ```razor
   @inject YourService Service
   ```

### Adding a New Dialog

1. Create component in `TSM31.Core/UI/` with `@bind-Hidden` pattern
2. Add reference in parent component
3. Create `Show()` method to toggle visibility
4. Call from F-key handler or button click

### Adding a New Main Tab

1. Add enum value to `MainTab` in `KeyboardNavigationService.cs`
2. Create component in `TSM31.Core/UI/`
3. Add navigation case in `TestStationLayout.razor`
4. Add menu item in `MenuComponent.razor`
5. Update function key handler

## Important Notes

- **Database Connection:** SQL Server connection is hardcoded. No appsettings.json configuration currently exists.
- **No Migrations:** Database schema is managed externally. DbContext was reverse-engineered.
- **Hot Reload:** Works well with Blazor. Splash screen logic detects hot reload and skips initialization.
- **Keyboard Focus:** Critical for industrial use. Test all keyboard shortcuts thoroughly.
- **Status Transitions:** `Aborted` tests are retryable; `Failed` and `Passed` are terminal states.
- **Multi-Test Units:** Units can have multiple tests (indexed by `CurrentTest` from 1 to `TotalTests`).
- **Legacy Code:** Original VB.NET code is documented in `docs/VB_TO_CSHARP_CONVERSION.md` and other docs for reference.

## Documentation

Extensive documentation in `docs/` directory:

- **ARCHITECTURE.md** - Visual diagrams of component hierarchy
- **FunctionKeyMap.md** - Comprehensive function key behavior reference
- **UnitDownload.md** - Detailed download flow documentation
- **KEYBOARD_SHORTCUTS.md** - Keyboard navigation reference
- **INITIALIZATION_STATE.md** - Splash screen behavior

Refer to these docs when making changes to navigation, test flows, or data download logic.

- Do not build project using `dotnet build` unless explicitly instructed to
