# FunctionKeyRegistry Service

## Overview

The function key system in TSM31 decouples keyboard shortcuts from UI components by routing every keystroke through a command registry. Components and services register the commands they own, and the `DielectricTestManager` resolves the active command for a key based on the current application context.

Key goals:
- Strongly typed registration and context matching
- Minimal boilerplate for common registrations
- Clear separation between command execution logic and key bindings
- Built-in support for async operations, confirmation flows, and UI messaging

## Core Types

### FunctionKeyRegistry (`src/TSM31.Core/Services/FunctionKeyRegistry.cs`)
- Thread-safe in-memory registry of `IFunctionKeyCommand` instances.
- Stores `(FunctionKey, FunctionKeyContext, Command, Owner)` tuples.
- Evaluates the best match for a key using context matching and specificity scoring.
- Surfaces utilities like `GetAllCommands`, `IsEnabled`, and `GetDiagnostics` for tooling and UI.
- Raises `OnRegistryChanged` when the set of registrations changes.

### FunctionKeyRegistrationScope
- Fluent helper returned by `FunctionKeyRegistry.BeginScope(owner)`.
- Tracks the owner automatically; calling `Dispose()` removes every command registered through the scope.
- Allows method-chained registrations:
  ```csharp
  _functionKeys = FunctionKeyRegistry.BeginScope(this)
      .Context(MainTab.DataEntry)
      .Command(new ReturnToMenuCommand(TestManager))
      .Command(FunctionKey.F1, "Download", HandleDownloadAsync, canExecute: () => CanDownload)
      .Command(FunctionKey.F2, "Clear", HandleClearAsync, tooltip: "Clear current unit (F2)");
  ```
- Supports multiple overloads: pass an `IFunctionKeyCommand`, a factory delegate, or inline async/sync actions.
- `Context(...)` can accept an existing `FunctionKeyContext`, a `MainTab`, or a builder delegate for fine-grained matching.

### FunctionKeyContext (`src/TSM31.Core/Models/FunctionKeyContext.cs`)
- Immutable record that describes when a command is valid (active tab, sub-tab, power state, etc.).
- `Matches(current)` determines compatibility; unspecified properties act as wildcards.
- `Specificity()` scores contexts so the most specific command wins when multiple matches exist.
- Helpers: `Any`, `ForTab`, `ForTabWithUnit`, and a fluent `FunctionKeyContextBuilder` for advanced scenarios.

### IFunctionKeyCommand & Base Implementations (`src/TSM31.Core/Models/FunctionKeyCommand.cs`)
- `IFunctionKeyCommand` is the contract consumed by the registry.
- `FunctionKeyCommandBase` handles async execution, error handling, and cancellation.
- `SyncFunctionKeyCommand` wraps synchronous logic.
- `DelegateCommand` allows fully inline commands when deriving a new class is unnecessary.

### CommandResult
- Unified return type for commands.
- Factory helpers clarify intent:
  - `CommandResult.Completed(message?, intent?)`
  - `CommandResult.Failed(message, intent?)`
  - `CommandResult.RequiresUserConfirmation(message, intent?)`
- `RequiresConfirmation` enables UI components to prompt the user and resume execution later (see `DownloadUnitCommand` and `ClearUnitCommand`).

## Typical Component Lifecycle

1. **Declare fields** for command instances and the registration scope.
   ```csharp
   private DownloadUnitCommand? _downloadCommand;
   private ClearUnitCommand? _clearCommand;
   private FunctionKeyRegistrationScope? _functionKeys;
   ```
2. **Create commands and register** inside `OnInitialized()`:
   ```csharp
   _downloadCommand = new DownloadUnitCommand(TestManager, () => _serialNumber);
   _clearCommand = new ClearUnitCommand(TestManager, () => _serialNumber);

   _functionKeys = FunctionKeyRegistry.BeginScope(this)
       .Context(MainTab.DataEntry)
       .Command(new ReturnToMenuCommand(TestManager))
       .Command(FunctionKey.F1, _downloadCommand.Label, HandleDownloadCommandAsync,
                canExecute: () => _downloadCommand.CanExecute(),
                tooltip: _downloadCommand.Tooltip)
       .Command(FunctionKey.F2, _clearCommand.Label, HandleClearCommandAsync,
                canExecute: () => _clearCommand.CanExecute(),
                tooltip: _clearCommand.Tooltip);
   ```
3. **Dispose** the scope when the component/service tears down:
   ```csharp
   _functionKeys?.Dispose();
   _functionKeys = null;
   ```

This approach keeps registration in one place, avoids manual `UnregisterAll` calls, and ensures the owner field stays consistent.

## Creating Commands

- Prefer deriving from `FunctionKeyCommandBase` for complex logic or when you need rich `CanExecute` behavior.
- Use `SyncFunctionKeyCommand` for quick synchronous operations (e.g., tab navigation).
- `DelegateCommand` is ideal for simple inline handlers or testing.
- Always return `CommandResult` using the factory helpers to express outcome, message intent, and confirmation requirements clearly.

### Example: Async Command with Confirmation
```csharp
public class DownloadUnitCommand : FunctionKeyCommandBase
{
    protected override async Task<CommandResult> ExecuteCoreAsync(CancellationToken token)
    {
        if (_testManager.CurrentUnit != null)
        {
            return CommandResult.RequiresUserConfirmation("Downloading will discard current data.");
        }

        var (success, message) = await _testManager.DownloadUnitAsync(_getPendingSerial());
        return success ? CommandResult.Completed(message) : CommandResult.Failed(message ?? "Download failed.");
    }
}
```

## Handling Confirmation in the UI

When a command returns `RequiresConfirmation`, the component decides how to continue:
1. Display a dialog using the message supplied by the command.
2. If the user confirms, call the command-specific continuation (e.g., `DownloadUnitCommand.PerformDownloadAsync`).
3. Provide feedback via `MessageService` or other UI mechanisms using the `CommandResult` metadata.

This keeps business logic inside commands while leaving UX decisions to the component.

## Dynamic Context Strategies

- Update context-sensitive commands by re-registering when application state changes (e.g., tab switches, unit download state).
- For temporary overrides, call `.Context(builder => builder.WithMainTab(...).WithUnitData())` before registering.
- Use multiple `Context(...)` calls in the same scope to group registrations by context.

## Integration with DielectricTestManager

`DielectricTestManager.HandleFunctionKeyAsync` converts raw key strings to `FunctionKey` values, builds the current `FunctionKeyContext`, and queries the registry. After executing the command it handles global messaging (status bar updates, errors). Components only need to ensure their commands reflect the desired state; the manager takes care of dispatch.

## Best Practices

- Own the scope in the component/service that created the registrations.
- Keep `CanExecute` logic fast; it is evaluated whenever the UI queries command status.
- Use strongly typed contexts to prevent accidental cross-tab bindings.
- Prefer reusable command classes for non-trivial behavior so you can unit-test them in isolation.
- Combine the fluent scope with record builders when matching requires multiple properties.

With the fluent registration scope and `CommandResult` helpers, assigning function key behavior typically fits in a single method-chained expression, minimizing boilerplate while keeping the system strongly typed and extensible.
