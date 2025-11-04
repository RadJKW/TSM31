# Function Key System Architecture

**Author:** Claude Code
**Date:** 2025-10-14
**Version:** 1.0

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture Components](#architecture-components)
3. [Creating Function Key Commands](#creating-function-key-commands)
4. [Registering Commands](#registering-commands)
5. [Context Matching](#context-matching)
6. [Enabling/Disabling Keys](#enablingdisabling-keys)
7. [Command Execution Flow](#command-execution-flow)
8. [Examples](#examples)
9. [Best Practices](#best-practices)
10. [Migration from Legacy System](#migration-from-legacy-system)

---

## Overview

The TSM31 application uses a **command-based function key system** that provides:

- **Context-aware key bindings** - Different actions for F1 depending on which tab is active
- **Decoupled architecture** - Commands are separate from UI components
- **Dynamic enable/disable** - Keys automatically enable/disable based on application state
- **Confirmation support** - Built-in confirmation dialogs for destructive operations
- **Thread-safe registry** - Commands can be registered/unregistered from any component

### Key Benefits

✅ **Maintainability** - Commands are isolated, testable classes
✅ **Flexibility** - Same key can trigger different commands in different contexts
✅ **Safety** - Built-in CanExecute logic prevents invalid operations
✅ **User Experience** - Automatic button enable/disable provides visual feedback

---

## Architecture Components

### 1. **IFunctionKeyCommand Interface**

The core interface that all commands must implement:

```csharp
public interface IFunctionKeyCommand
{
    FunctionKey Key { get; }           // Which key triggers this command
    string Label { get; }              // Display text (e.g., "Download")
    string? Tooltip { get; }           // Optional help text
    bool CanExecute();                 // Can this command run right now?
    Task<CommandResult> ExecuteAsync(CancellationToken ct);
}
```

**Location:** `TSM31.Core/Services/FunctionKeyCommand.cs:9-39`

---

### 2. **FunctionKeyContext Record**

Describes the current application state for command matching:

```csharp
public record FunctionKeyContext
{
    public MainTab? MainTab { get; init; }          // DataEntry, Hipot, etc.
    public string? SubTab { get; init; }            // "Primary", "Secondary", etc.
    public bool HasUnitData { get; init; }          // Has a unit been downloaded?
    public bool TestPowerOn { get; init; }          // Is test power enabled?
    public int? CurrentTestNumber { get; init; }    // 1-based test index
    public int? TotalTests { get; init; }           // Total tests for unit
}
```

**Location:** `TSM31.Core/Services/FunctionKeyContext.cs:13-120`

**Key Methods:**
- `Matches(FunctionKeyContext current)` - Determines if this context matches the current state
- `Specificity()` - Calculates priority score (more specific = higher priority)
- `ForTab(MainTab tab)` - Factory method for tab-specific contexts
- `ForTabWithUnit(MainTab tab)` - Factory method requiring unit data

---

### 3. **FunctionKeyRegistry Class**

Thread-safe registry that manages all command registrations:

```csharp
public class FunctionKeyRegistry
{
    // Register a command for a specific context
    void Register(FunctionKey key, FunctionKeyContext context,
                  IFunctionKeyCommand command, object owner);

    // Get the best matching command for current state
    IFunctionKeyCommand? GetCommand(FunctionKey key,
                                    FunctionKeyContext currentContext);

    // Check if a key is enabled in current context
    bool IsEnabled(FunctionKey key, FunctionKeyContext currentContext);

    // Remove all commands registered by an owner (cleanup)
    void UnregisterAll(object owner);
}
```

**Location:** `TSM31.Core/Services/FunctionKeyRegistry.cs:10-211`

**Registry Changed Event:**
```csharp
public event Action? OnRegistryChanged;  // Fired when commands are added/removed
```

---

### 4. **FunctionKeyCommandBase Abstract Class**

Base class providing common functionality for all commands:

```csharp
public abstract class FunctionKeyCommandBase : IFunctionKeyCommand
{
    protected FunctionKeyCommandBase(FunctionKey key, string label,
                                     string? tooltip = null);

    public virtual bool CanExecute() => true;

    // Template method - handles errors, cancellation, logging
    public async Task<CommandResult> ExecuteAsync(CancellationToken ct);

    // Derived classes implement this
    protected abstract Task<CommandResult> ExecuteCoreAsync(CancellationToken ct);
}
```

**Location:** `TSM31.Core/Services/FunctionKeyCommand.cs:79-129`

**Features:**
- ✅ Automatic error handling (converts exceptions to `CommandResult`)
- ✅ Cancellation support (`OperationCanceledException` handling)
- ✅ Console logging for debugging
- ✅ Template method pattern - override `ExecuteCoreAsync()` only

---

### 5. **CommandResult Record**

Return value from command execution:

```csharp
public record CommandResult(
    bool Success,                           // Did it work?
    string? Message = null,                 // Optional status message
    MessageIntent Intent = MessageIntent.Info,  // Info/Warning/Error/Success
    bool RequiresConfirmation = false       // Show confirmation dialog?
);
```

**Location:** `TSM31.Core/Services/FunctionKeyCommand.cs:48-53`

---

## Creating Function Key Commands

### Option 1: Inherit from `FunctionKeyCommandBase` (Recommended)

Use this approach for complex commands with business logic.

**Example: Download Unit Command**

```csharp
using Microsoft.FluentUI.AspNetCore.Components;

namespace TSM31.Core.Services.Commands;

public class DownloadUnitCommand : FunctionKeyCommandBase
{
    private readonly DielectricTestManager _testManager;
    private readonly Func<string?> _getPendingSerial;

    public DownloadUnitCommand(
        DielectricTestManager testManager,
        Func<string?> getPendingSerial)
        : base(FunctionKey.F1, "Download", "Download unit data from database (F1)")
    {
        _testManager = testManager;
        _getPendingSerial = getPendingSerial;
    }

    public override bool CanExecute()
    {
        // Enable button only if serial number is valid
        var serial = _getPendingSerial();
        return _testManager.ValidateSerialNumber(serial);
    }

    protected override async Task<CommandResult> ExecuteCoreAsync(
        CancellationToken cancellationToken)
    {
        var serial = _getPendingSerial();

        if (string.IsNullOrWhiteSpace(serial))
        {
            return new CommandResult(false,
                "Please enter a serial number.",
                MessageIntent.Warning);
        }

        // Request confirmation if unit already exists
        if (_testManager.CurrentUnit != null)
        {
            return new CommandResult(
                Success: false,
                Message: "Unit already downloaded. Replace it?",
                Intent: MessageIntent.Warning,
                RequiresConfirmation: true
            );
        }

        // Perform the download
        var (success, message) = await _testManager.DownloadUnitAsync(serial);

        return new CommandResult(success, message,
            success ? MessageIntent.Success : MessageIntent.Error);
    }
}
```

**File Reference:** `TSM31.Core/Services/Commands/DownloadUnitCommand.cs`

---

### Option 2: Use `SyncFunctionKeyCommand` for Synchronous Commands

For commands that don't require async operations:

```csharp
public class ToggleTestPowerCommand : SyncFunctionKeyCommand
{
    private readonly DielectricTestManager _testManager;

    public ToggleTestPowerCommand(DielectricTestManager testManager)
        : base(FunctionKey.F1, "Toggle Power", "Toggle test power on/off")
    {
        _testManager = testManager;
    }

    public override bool CanExecute()
    {
        // Only allow when unit is downloaded
        return _testManager.CurrentUnit != null;
    }

    protected override CommandResult ExecuteCore()
    {
        _testManager.ToggleTestPower();
        var state = _testManager.TestPowerOn ? "ON" : "OFF";
        return new CommandResult(true, $"Test power is now {state}",
            MessageIntent.Info);
    }
}
```

---

### Option 3: Use `DelegateCommand` for Simple Inline Commands

For quick, one-off commands without creating a separate class:

```csharp
var escapeCommand = new DelegateCommand(
    FunctionKey.Esc,
    "Home",
    async (ct) =>
    {
        _testManager.SetMainTab(MainTab.None);
        await Task.CompletedTask;
        return new CommandResult(true, "Returned to home", MessageIntent.Info);
    },
    canExecute: () => _testManager.CurrentMainTab != MainTab.None,
    tooltip: "Return to home screen (ESC)"
);
```

---

## Registering Commands

### When to Register Commands

Commands should be registered:
- **Component Initialization** - In `OnInitialized()` lifecycle method
- **Service Startup** - In service constructors (e.g., `DielectricTestManager`)
- **Dynamically** - When certain features become available

### Registration Examples

#### 1. Register in Component (Blazor)

```csharp
@inject FunctionKeyRegistry KeyRegistry
@inject DielectricTestManager TestManager
@implements IDisposable

@code {
    private IFunctionKeyCommand? _downloadCommand;

    protected override void OnInitialized()
    {
        // Create command instance
        _downloadCommand = new DownloadUnitCommand(
            TestManager,
            () => _serialNumber);

        // Register for DataEntry tab only
        var context = FunctionKeyContext.ForTab(MainTab.DataEntry);
        KeyRegistry.Register(FunctionKey.F1, context, _downloadCommand, this);

        // Alternative: Register with builder for more complex context
        var complexContext = FunctionKeyContext.Builder()
            .WithMainTab(MainTab.Hipot)
            .WithSubTab("Primary")
            .WithUnitData(true)
            .Build();

        KeyRegistry.Register(FunctionKey.F3, complexContext,
            _startTestCommand, this);
    }

    public void Dispose()
    {
        // IMPORTANT: Clean up all registrations when component disposes
        KeyRegistry.UnregisterAll(this);
    }
}
```

---

#### 2. Register in Service Constructor

```csharp
public class DielectricTestManager
{
    private readonly FunctionKeyRegistry _keyRegistry;

    public DielectricTestManager(FunctionKeyRegistry keyRegistry, ...)
    {
        _keyRegistry = keyRegistry;

        // Register global commands (available everywhere)
        RegisterGlobalCommands();
    }

    private void RegisterGlobalCommands()
    {
        // ESC - Return to home (works from any context)
        _keyRegistry.RegisterSyncAction(
            FunctionKey.Esc,
            FunctionKeyContext.Any,  // Wildcard - matches everything
            "Home",
            () =>
            {
                SetMainTab(MainTab.None);
                return new CommandResult(true, "Returned to home");
            },
            this,
            canExecute: () => CurrentMainTab != MainTab.None,
            tooltip: "Return to home screen (ESC)"
        );
    }
}
```

---

#### 3. Register with Extension Methods

The `FunctionKeyRegistryExtensions` class provides convenience methods:

```csharp
// Simple async action
KeyRegistry.RegisterAction(
    FunctionKey.F8,
    FunctionKeyContext.ForTab(MainTab.DataEntry),
    "Upload Data",
    async (ct) =>
    {
        await UploadDataAsync(ct);
        return new CommandResult(true, "Data uploaded");
    },
    owner: this,
    canExecute: () => HasDataToUpload,
    tooltip: "Upload test data to server (F8)"
);

// Simple sync action
KeyRegistry.RegisterSyncAction(
    FunctionKey.F7,
    FunctionKeyContext.Any,
    "Enter Operator ID",
    () =>
    {
        ShowOperatorDialog();
        return new CommandResult(true);
    },
    owner: this
);
```

**Location:** `TSM31.Core/Services/FunctionKeyRegistry.cs:216-256`

---

## Context Matching

### How Context Matching Works

When a function key is pressed:

1. **Get Current Context** - Application builds a `FunctionKeyContext` with current state
2. **Find Matching Commands** - Registry searches for all commands registered for that key
3. **Filter by Context** - Only commands whose context `.Matches()` the current context are kept
4. **Sort by Specificity** - Most specific command (highest score) wins
5. **Execute Command** - The winning command is executed

### Context Matching Rules

A command's context **matches** the current context if:

| Property | Match Rule |
|----------|-----------|
| `MainTab` | Must match exactly if specified, or acts as wildcard if `null` |
| `SubTab` | Must match exactly if specified, or acts as wildcard if `null` |
| `HasUnitData` | If `true` in command context, current must also be `true` |
| `TestPowerOn` | Must match exactly if `true` in command context |
| `CurrentTestNumber` | Must match exactly if specified |
| `TotalTests` | Must match exactly if specified |

**Wildcards:** A `null` or `false` value in the command's context acts as a wildcard and matches any value.

### Specificity Scoring

When multiple commands match, the most specific one is chosen:

```csharp
public int Specificity()
{
    int score = 0;
    if (MainTab.HasValue) score += 10;      // Most important
    if (SubTab != null) score += 5;
    if (HasUnitData) score += 3;
    if (TestPowerOn) score += 2;
    if (CurrentTestNumber.HasValue) score += 2;
    if (TotalTests.HasValue) score += 1;    // Least important
    return score;
}
```

**Example:**

Given these three F1 commands:

| Command | Context | Specificity |
|---------|---------|-------------|
| A | `{ MainTab = DataEntry, SubTab = "Primary" }` | 15 points |
| B | `{ MainTab = DataEntry }` | 10 points |
| C | `{ MainTab = null }` (wildcard) | 0 points |

If current context is `{ MainTab = DataEntry, SubTab = "Primary" }`:
- ✅ All three commands match
- 🏆 **Command A wins** (most specific with 15 points)

---

### Context Examples

```csharp
// Example 1: Global command (works everywhere)
var globalContext = FunctionKeyContext.Any;
KeyRegistry.Register(FunctionKey.Esc, globalContext, escapeCmd, this);

// Example 2: Tab-specific command
var dataEntryContext = FunctionKeyContext.ForTab(MainTab.DataEntry);
KeyRegistry.Register(FunctionKey.F1, dataEntryContext, downloadCmd, this);

// Example 3: Requires unit data
var hipotContext = FunctionKeyContext.ForTabWithUnit(MainTab.Hipot);
KeyRegistry.Register(FunctionKey.F3, hipotContext, startTestCmd, this);

// Example 4: Complex context with builder
var complexContext = FunctionKeyContext.Builder()
    .WithMainTab(MainTab.Hipot)
    .WithSubTab("Primary")
    .WithUnitData(true)
    .WithTestPower(false)  // Only when power is OFF
    .Build();
KeyRegistry.Register(FunctionKey.F1, complexContext, powerOnCmd, this);

// Example 5: Test-specific context
var multiTestContext = new FunctionKeyContext
{
    MainTab = MainTab.Impulse,
    CurrentTestNumber = 2,
    TotalTests = 3
};
KeyRegistry.Register(FunctionKey.F10, multiTestContext, nextTestCmd, this);
```

---

## Enabling/Disabling Keys

### Automatic Enable/Disable via `CanExecute()`

The `CanExecute()` method determines if a command can run:

```csharp
public override bool CanExecute()
{
    // Example: Enable F1 (Download) only when serial is valid
    var serial = _getPendingSerial();
    return !string.IsNullOrWhiteSpace(serial) &&
           serial.Length >= 3 &&
           serial.All(char.IsDigit);
}
```

**When `CanExecute()` returns `false`:**
- ❌ Command cannot execute
- 🔘 UI buttons are automatically disabled
- ⚠️ Pressing the key shows: *"Command cannot execute in current state"*

---

### UI Integration with `FunctionKeyMap`

The `DielectricTestManager` maintains a `FunctionKeyMap` dictionary for legacy compatibility:

```csharp
private readonly ConcurrentDictionary<FunctionKey, bool> FunctionKeyMap = new();

private void SetFunctionKey(FunctionKey key, bool enabled)
{
    FunctionKeyMap.AddOrUpdate(key, enabled, (_, _) => enabled);
    OnFunctionKeysChanged?.Invoke();  // Notify UI
}
```

**Usage in Menu Components:**

```csharp
@foreach (var key in AllKeys)
{
    var isEnabled = TestManager.FunctionKeyMap[key];

    <MenuButton KeyText="@key.ToString()"
                Label="@GetLabel(key)"
                IsDisabled="@(!isEnabled)"
                OnClick="@(() => HandleKeyPress(key))"/>
}
```

---

### Refreshing Key States

Key states are refreshed automatically when:

1. **Unit Data Changes** - Download completes, unit cleared
2. **Tab Changes** - User navigates to different main/sub tab
3. **Manual Refresh** - Call `RefreshFunctionKeysContext()` after state changes

**Example: Manual Refresh**

```csharp
public void UpdatePendingSerial(string? serial)
{
    PendingSerialNumber = serial;
    RefreshFunctionKeysContext();  // Recalculate which keys are enabled
    OnFunctionKeysChanged?.Invoke();  // Notify UI to update button states
}
```

---

## Command Execution Flow

### 1. User Presses Function Key

```
[User presses F1]
    ↓
[Global keyboard handler captures key]
    ↓
[TestStationLayout.OnGlobalKey("F1") is invoked via JSInterop]
    ↓
[Calls TestManager.HandleFunctionKeyAsync("F1")]
```

---

### 2. Command Lookup and Execution

```csharp
public async Task HandleFunctionKeyAsync(string rawKey)
{
    // 1. Parse key string to enum
    var functionKey = ParseFunctionKey(rawKey);  // "F1" → FunctionKey.F1
    if (functionKey == null) return;

    // 2. Get current application context
    var context = GetCurrentContext();
    // Returns: { MainTab = DataEntry, HasUnitData = false, ... }

    // 3. Find matching command from registry
    var command = _keyRegistry.GetCommand(functionKey.Value, context);
    if (command == null)
    {
        Console.WriteLine($"No command registered for {functionKey}");
        return;
    }

    // 4. Execute the command
    var result = await command.ExecuteAsync();

    // 5. Handle the result
    await HandleCommandResultAsync(result, command);
}
```

**Location:** `DielectricTestManager.cs:135-169`

---

### 3. Result Handling

```csharp
private async Task HandleCommandResultAsync(CommandResult result,
                                            IFunctionKeyCommand command)
{
    // If confirmation required, show dialog
    if (result.RequiresConfirmation)
    {
        RequestConfirmation(result.Message ?? "Confirm action?", command);
        return;
    }

    // Show status message to user
    if (!string.IsNullOrWhiteSpace(result.Message))
    {
        OnStatus?.Invoke(result.Message, result.Intent);
    }
}
```

**Location:** `DielectricTestManager.cs:228-244`

---

### 4. Confirmation Flow

For commands that return `RequiresConfirmation = true`:

```
[Command returns RequiresConfirmation = true]
    ↓
[OnConfirmationRequested event is raised]
    ↓
[DataEntryTab shows ConfirmationDialog]
    ↓
[User clicks "Yes" or "No"]
    ↓
[If "Yes": Command's PerformXXXAsync() method is called]
    ↓
[Result is shown to user]
```

**Example: Download Command with Confirmation**

```csharp
protected override async Task<CommandResult> ExecuteCoreAsync(
    CancellationToken cancellationToken)
{
    // Check if unit already exists
    if (_testManager.CurrentUnit != null)
    {
        // Request confirmation
        return new CommandResult(
            Success: false,
            Message: "Unit already downloaded. Replace it?",
            Intent: MessageIntent.Warning,
            RequiresConfirmation: true  // ← This triggers dialog
        );
    }

    // No confirmation needed, perform download directly
    return await PerformDownloadAsync(serialNumber, cancellationToken);
}

// Called after user confirms
public async Task<CommandResult> PerformDownloadAsync(string serialNumber,
                                                      CancellationToken ct)
{
    var (success, message) = await _testManager.DownloadUnitAsync(serialNumber);
    return new CommandResult(success, message);
}
```

---

## Examples

### Example 1: Navigation Command (Escape Key)

```csharp
public class ReturnToHomeCommand : SyncFunctionKeyCommand
{
    private readonly DielectricTestManager _testManager;

    public ReturnToHomeCommand(DielectricTestManager testManager)
        : base(FunctionKey.Esc, "Home", "Return to home screen (ESC)")
    {
        _testManager = testManager;
    }

    public override bool CanExecute()
    {
        // Only enabled when NOT already on home screen
        return _testManager.CurrentMainTab != MainTab.None;
    }

    protected override CommandResult ExecuteCore()
    {
        _testManager.SetMainTab(MainTab.None);
        return new CommandResult(true, "Returned to home screen",
            MessageIntent.Info);
    }
}

// Register globally (works from any tab)
var context = FunctionKeyContext.Any;
KeyRegistry.Register(FunctionKey.Esc, context, new ReturnToHomeCommand(TestManager), this);
```

---

### Example 2: Data Entry Commands (F1 Download, F2 Clear)

```csharp
// In DataEntryTab.razor.cs or component code
protected override void OnInitialized()
{
    var dataEntryContext = FunctionKeyContext.ForTab(MainTab.DataEntry);

    // F1 - Download
    _downloadCommand = new DownloadUnitCommand(TestManager, () => _serialNumber);
    KeyRegistry.Register(FunctionKey.F1, dataEntryContext, _downloadCommand, this);

    // F2 - Clear
    _clearCommand = new ClearUnitCommand(TestManager, () => _serialNumber);
    KeyRegistry.Register(FunctionKey.F2, dataEntryContext, _clearCommand, this);
}

public void Dispose()
{
    KeyRegistry.UnregisterAll(this);
}
```

---

### Example 3: Test Power Control with Context

```csharp
// Power ON command - only available when power is OFF
var powerOffContext = new FunctionKeyContext
{
    MainTab = MainTab.Hipot,
    TestPowerOn = false,  // Only when power is off
    HasUnitData = true    // Requires unit downloaded
};

var powerOnCommand = new DelegateCommand(
    FunctionKey.F1,
    "Power ON",
    async (ct) =>
    {
        _testManager.SetTestPower(true);
        await Task.CompletedTask;
        return new CommandResult(true, "Test power enabled", MessageIntent.Success);
    },
    tooltip: "Turn on test power (F1)"
);

KeyRegistry.Register(FunctionKey.F1, powerOffContext, powerOnCommand, this);

// Power OFF command - only available when power is ON
var powerOnContext = new FunctionKeyContext
{
    MainTab = MainTab.Hipot,
    TestPowerOn = true,   // Only when power is on
    HasUnitData = true
};

var powerOffCommand = new DelegateCommand(
    FunctionKey.F1,
    "Power OFF",
    async (ct) =>
    {
        _testManager.SetTestPower(false);
        await Task.CompletedTask;
        return new CommandResult(true, "Test power disabled", MessageIntent.Warning);
    },
    tooltip: "Turn off test power (F1)"
);

KeyRegistry.Register(FunctionKey.F1, powerOnContext, powerOffCommand, this);
```

**Result:** F1 toggles power state, button label changes automatically!

---

### Example 4: Multi-Test Navigation

```csharp
public class NextTestCommand : SyncFunctionKeyCommand
{
    private readonly DielectricTestManager _testManager;

    public NextTestCommand(DielectricTestManager testManager)
        : base(FunctionKey.F10, "Next Test", "Advance to next test (F10)")
    {
        _testManager = testManager;
    }

    public override bool CanExecute()
    {
        var unit = _testManager.CurrentUnit;
        if (unit == null) return false;

        // Enable if not on last test
        return unit.CurrentTest < unit.TotalTests;
    }

    protected override CommandResult ExecuteCore()
    {
        var unit = _testManager.CurrentUnit!;
        unit.CurrentTest++;

        return new CommandResult(true,
            $"Moved to test {unit.CurrentTest} of {unit.TotalTests}",
            MessageIntent.Info);
    }
}
```

---

### Example 5: Tab-Specific Sub-Menu Commands

```csharp
// Hipot tab has sub-tabs: Simultaneous, Primary, Secondary, 4LVB
// Each F-key switches to a different sub-tab

var hipotContext = FunctionKeyContext.ForTab(MainTab.Hipot);

// F1 - Simultaneous
KeyRegistry.RegisterSyncAction(
    FunctionKey.F1,
    hipotContext,
    "Simultaneous",
    () =>
    {
        _testManager.SetSubTab("Simultaneous");
        return new CommandResult(true);
    },
    this
);

// F2 - Primary
KeyRegistry.RegisterSyncAction(
    FunctionKey.F2,
    hipotContext,
    "Primary",
    () =>
    {
        _testManager.SetSubTab("Primary");
        return new CommandResult(true);
    },
    this
);

// F3 - Secondary
KeyRegistry.RegisterSyncAction(
    FunctionKey.F3,
    hipotContext,
    "Secondary",
    () =>
    {
        _testManager.SetSubTab("Secondary");
        return new CommandResult(true);
    },
    this
);

// F4 - 4LVB
KeyRegistry.RegisterSyncAction(
    FunctionKey.F4,
    hipotContext,
    "4LVB",
    () =>
    {
        _testManager.SetSubTab("4LVB");
        return new CommandResult(true);
    },
    this
);
```

---

## Best Practices

### ✅ DO

1. **Always unregister commands in `Dispose()`**
   ```csharp
   public void Dispose()
   {
       KeyRegistry.UnregisterAll(this);
   }
   ```

2. **Use specific contexts to avoid conflicts**
   ```csharp
   // Good: Specific to DataEntry tab
   var context = FunctionKeyContext.ForTab(MainTab.DataEntry);

   // Bad: Too broad, might conflict
   var context = FunctionKeyContext.Any;
   ```

3. **Implement `CanExecute()` for proper enable/disable**
   ```csharp
   public override bool CanExecute()
   {
       return _testManager.CurrentUnit != null &&
              !string.IsNullOrEmpty(_pendingData);
   }
   ```

4. **Use confirmation for destructive operations**
   ```csharp
   if (_testManager.HasUnsavedData())
   {
       return new CommandResult(
           Success: false,
           Message: "You have unsaved data. Continue?",
           RequiresConfirmation: true
       );
   }
   ```

5. **Provide meaningful labels and tooltips**
   ```csharp
   base(FunctionKey.F1,
        "Download",  // Short label for button
        "Download unit data from SQL Server database (F1)")  // Helpful tooltip
   ```

6. **Use `FunctionKeyCommandBase` for complex commands**
   - Inherit from base class for automatic error handling
   - Override `ExecuteCoreAsync` for business logic
   - Override `CanExecute` for enable/disable logic

7. **Use extension methods for simple commands**
   ```csharp
   KeyRegistry.RegisterSyncAction(
       FunctionKey.F7,
       FunctionKeyContext.Any,
       "Operator ID",
       () => ShowDialog()
   );
   ```

---

### ❌ DON'T

1. **Don't register the same key twice for the same context**
   ```csharp
   // BAD: Both will match, causing confusion
   KeyRegistry.Register(FunctionKey.F1, dataEntryContext, cmdA, this);
   KeyRegistry.Register(FunctionKey.F1, dataEntryContext, cmdB, this);  // ❌
   ```

2. **Don't forget to dispose/unregister**
   ```csharp
   // BAD: Memory leak! Commands stay registered after component is destroyed
   protected override void OnInitialized()
   {
       KeyRegistry.Register(...);
   }
   // Missing Dispose() method! ❌
   ```

3. **Don't perform UI updates in command execution**
   ```csharp
   // BAD: Commands should not directly manipulate UI
   protected override CommandResult ExecuteCore()
   {
       _blazorComponent.StateHasChanged();  // ❌ Wrong layer
       return new CommandResult(true);
   }

   // GOOD: Return result, let UI handle display
   protected override CommandResult ExecuteCore()
   {
       return new CommandResult(true, "Operation complete", MessageIntent.Success);
   }
   ```

4. **Don't use blocking calls in async commands**
   ```csharp
   // BAD
   protected override async Task<CommandResult> ExecuteCoreAsync(CancellationToken ct)
   {
       Thread.Sleep(5000);  // ❌ Blocks UI thread
       return new CommandResult(true);
   }

   // GOOD
   protected override async Task<CommandResult> ExecuteCoreAsync(CancellationToken ct)
   {
       await Task.Delay(5000, ct);  // ✅ Non-blocking
       return new CommandResult(true);
   }
   ```

5. **Don't ignore the cancellation token**
   ```csharp
   // BAD: Ignores cancellation
   protected override async Task<CommandResult> ExecuteCoreAsync(CancellationToken ct)
   {
       await LongRunningOperationAsync();  // ❌ No token passed
       return new CommandResult(true);
   }

   // GOOD: Respects cancellation
   protected override async Task<CommandResult> ExecuteCoreAsync(CancellationToken ct)
   {
       await LongRunningOperationAsync(ct);  // ✅ Cancellable
       return new CommandResult(true);
   }
   ```

6. **Don't throw exceptions from commands**
   ```csharp
   // BAD: Exceptions escape to caller
   protected override CommandResult ExecuteCore()
   {
       if (!IsValid())
           throw new InvalidOperationException("Invalid state");  // ❌
       return new CommandResult(true);
   }

   // GOOD: Return error result
   protected override CommandResult ExecuteCore()
   {
       if (!IsValid())
           return new CommandResult(false, "Invalid state", MessageIntent.Error);  // ✅
       return new CommandResult(true);
   }
   ```

---

## Migration from Legacy System

### Old System (Before Refactoring)

The old system used a monolithic `HandleFunctionKey` method with large switch statements:

```csharp
// OLD APPROACH ❌
public void HandleFunctionKey(string key)
{
    switch (key)
    {
        case "F1":
            if (CurrentMainTab == MainTab.DataEntry)
            {
                if (ValidateSerialNumber(_pendingSerial))
                {
                    DownloadUnit(_pendingSerial);
                }
            }
            else if (CurrentMainTab == MainTab.Hipot)
            {
                if (CurrentSubTab == "Primary")
                {
                    StartPrimaryHipot();
                }
            }
            // ... hundreds more lines
            break;
        case "F2":
            // More switch logic...
            break;
        // ... F3-F12
    }
}
```

**Problems:**
- ❌ Not testable (monolithic logic)
- ❌ Hard to maintain (one giant method)
- ❌ Tight coupling (UI, business logic, navigation all mixed)
- ❌ No enable/disable support
- ❌ Difficult to add new commands

---

### New System (After Refactoring)

```csharp
// NEW APPROACH ✅
public async Task HandleFunctionKeyAsync(string rawKey)
{
    var functionKey = ParseFunctionKey(rawKey);
    if (functionKey == null) return;

    var context = GetCurrentContext();
    var command = _keyRegistry.GetCommand(functionKey.Value, context);

    if (command == null) return;

    var result = await command.ExecuteAsync();
    await HandleCommandResultAsync(result, command);
}
```

**Benefits:**
- ✅ Testable (isolated command classes)
- ✅ Maintainable (single responsibility)
- ✅ Decoupled (commands don't know about UI)
- ✅ Automatic enable/disable via `CanExecute()`
- ✅ Easy to extend (just add new command classes)

---

### Migration Checklist

When migrating old function key logic:

1. ☐ Identify the action performed by the key
2. ☐ Determine the context (which tab/state)
3. ☐ Create a command class inheriting from `FunctionKeyCommandBase`
4. ☐ Implement `CanExecute()` based on old validation logic
5. ☐ Implement `ExecuteCoreAsync()` with the old action logic
6. ☐ Register the command with appropriate context
7. ☐ Test the command in isolation (unit tests)
8. ☐ Remove old switch case from legacy handler
9. ☐ Update UI button bindings if needed
10. ☐ Add documentation/comments

---

### Example Migration: F8 Upload Command

**Before (Legacy):**
```csharp
case "F8":
    if (CurrentMainTab == MainTab.DataEntry && CurrentUnit != null)
    {
        if (!ValidateTestData())
        {
            ShowMessage("Test data incomplete", MessageIntent.Warning);
            return;
        }

        try
        {
            UploadDataToServer(CurrentUnit);
            ShowMessage("Upload complete", MessageIntent.Success);
        }
        catch (Exception ex)
        {
            ShowMessage($"Upload failed: {ex.Message}", MessageIntent.Error);
        }
    }
    break;
```

**After (New System):**
```csharp
public class UploadDataCommand : FunctionKeyCommandBase
{
    private readonly DielectricTestManager _testManager;

    public UploadDataCommand(DielectricTestManager testManager)
        : base(FunctionKey.F8, "Upload Data", "Upload test data to server (F8)")
    {
        _testManager = testManager;
    }

    public override bool CanExecute()
    {
        return _testManager.CurrentUnit != null &&
               _testManager.ValidateTestData();
    }

    protected override async Task<CommandResult> ExecuteCoreAsync(CancellationToken ct)
    {
        if (!_testManager.ValidateTestData())
        {
            return new CommandResult(false,
                "Test data incomplete",
                MessageIntent.Warning);
        }

        try
        {
            await _testManager.UploadDataToServerAsync(_testManager.CurrentUnit!, ct);
            return new CommandResult(true,
                "Upload complete",
                MessageIntent.Success);
        }
        catch (Exception ex)
        {
            return new CommandResult(false,
                $"Upload failed: {ex.Message}",
                MessageIntent.Error);
        }
    }
}

// Registration
var context = FunctionKeyContext.ForTabWithUnit(MainTab.DataEntry);
KeyRegistry.Register(FunctionKey.F8, context, new UploadDataCommand(TestManager), this);
```

---

## Summary

The new function key system provides:

| Feature | Old System | New System |
|---------|-----------|------------|
| **Architecture** | Monolithic switch | Command pattern |
| **Testability** | ❌ Hard to test | ✅ Isolated command classes |
| **Maintainability** | ❌ One giant method | ✅ Single responsibility |
| **Enable/Disable** | ❌ Manual tracking | ✅ Automatic via `CanExecute()` |
| **Context Awareness** | ❌ Nested if statements | ✅ Context matching |
| **Extensibility** | ❌ Modify switch | ✅ Add command class |
| **Error Handling** | ❌ Try-catch everywhere | ✅ Centralized in base class |
| **Confirmation** | ❌ Manual dialogs | ✅ Built-in via `CommandResult` |
| **Threading** | ❌ Blocking calls | ✅ Full async support |

---

## Quick Reference

### Key Classes and Files

| Class/Interface | File Location | Purpose |
|----------------|---------------|---------|
| `IFunctionKeyCommand` | `Services/FunctionKeyCommand.cs` | Command interface |
| `FunctionKeyCommandBase` | `Services/FunctionKeyCommand.cs` | Base class for commands |
| `FunctionKeyRegistry` | `Services/FunctionKeyRegistry.cs` | Command registry |
| `FunctionKeyContext` | `Services/FunctionKeyContext.cs` | Context matching |
| `DielectricTestManager` | `Services/DielectricTestManager.cs` | Main orchestrator |
| `DownloadUnitCommand` | `Services/Commands/DownloadUnitCommand.cs` | Example command |
| `ClearUnitCommand` | `Services/Commands/ClearUnitCommand.cs` | Example command |

---

## Further Reading

- **Command Pattern:** [Refactoring Guru - Command](https://refactoring.guru/design-patterns/command)
- **Async Best Practices:** [Microsoft Docs - Async/Await](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/)
- **Dependency Injection:** [Microsoft Docs - DI in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

---

**Questions or Issues?**
Contact the development team or create an issue in the project repository.

**Last Updated:** 2025-10-14
**Version:** 1.0
