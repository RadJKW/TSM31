# Message Console Service - Usage Guide

The `MessageConsoleService` provides a centralized logging/messaging console that appears at the bottom of the application. It collects all messages (info, warnings, errors, traces, instructions, etc.) and displays them in a scrollable terminal-like interface.

## Features

- **Multiple Message Levels**: Info, Warning, Error, Success, Debug, Trace, Instruction, Event
- **Color-Coded Display**: Each level has a distinct color for easy scanning
- **Auto-Scroll**: Optional automatic scrolling to latest messages
- **Message History**: Maintains up to 1000 messages (configurable)
- **Filtering**: Can retrieve messages by level or category
- **Clear Console**: Button to clear all messages

## Usage Examples

### Basic Usage

```csharp
@inject MessageConsoleService MessageConsole

// In your component code:

// Add info message
MessageConsole.AddInfo("Unit data loaded successfully", "DataEntry");

// Add warning
MessageConsole.AddWarning("Meter timeout approaching", "Hipot");

// Add error
MessageConsole.AddError("Failed to download unit data", "Download");

// Add success
MessageConsole.AddSuccess("Test completed successfully", "Impulse");

// Add debug/trace (hidden in production)
MessageConsole.AddDebug("Variable X = 123", "Debug");
MessageConsole.AddTrace("Entering function GetData()", "Trace");

// Add instruction
MessageConsole.AddInstruction("Press F1 to download unit data", "Help");

// Add event
MessageConsole.AddEvent("User logged in", "Auth");
```

### Message Format

Each message displays as:
```
[HH:mm:ss] [LEVEL] (Category) Message text
```

Example outputs:
```
[14:35:22] [INFO] (DataEntry) Unit data loaded successfully
[14:35:23] [WARN] (Hipot) Meter timeout approaching
[14:35:24] [ERROR] (Download) Failed to download unit data
[14:35:25] [OK] (Impulse) Test completed successfully
[14:35:26] [INSTR] (Help) Press F1 to download unit data
```

### Color Mapping

- **Info**: White text
- **Warning**: Yellow text
- **Error**: Red text
- **Success**: Green text
- **Debug**: Blue text
- **Trace**: Gray text
- **Instruction**: Cyan text
- **Event**: Default gray

### Using in Services

```csharp
public class DielectricTestManager
{
    private readonly MessageConsoleService _console;
    
    public DielectricTestManager(MessageConsoleService console)
    {
        _console = console;
    }
    
    public async Task DownloadUnitAsync(string serialNumber)
    {
        try
        {
            _console.AddInfo($"Downloading unit {serialNumber}...", "Download");
            // ... download logic ...
            _console.AddSuccess($"Unit {serialNumber} downloaded", "Download");
        }
        catch (Exception ex)
        {
            _console.AddError($"Download failed: {ex.Message}", "Download");
        }
    }
}
```

### Querying Messages

```csharp
// Get all error messages
var errors = MessageConsole.GetMessagesByLevel(MessageLevel.Error);

// Get messages from a specific category
var dataEntryMsgs = MessageConsole.GetMessagesByCategory("DataEntry");

// Get last 10 messages
var recent = MessageConsole.GetRecentMessages(10);

// Get all messages
var all = MessageConsole.Messages;
```

### Clear Console

```csharp
// Programmatically
MessageConsole.Clear();

// Via UI (Clear button in console header)
```

### Auto-Scroll Toggle

Users can toggle between Auto-Scroll and Manual modes using the "Auto/Manual" button in the console header.

## Console Display

The console appears as a fixed 128px height (h-32) black terminal at the bottom of the application with:
- Header showing "Console Output" label
- Clear button to reset messages
- Auto/Manual toggle for scroll behavior
- Scrollable message area with monospace font
- Color-coded messages based on level

## Best Practices

1. **Use Categories**: Always provide a category to help users understand which feature/module generated the message
2. **Be Concise**: Keep messages short and actionable
3. **Use Appropriate Levels**: 
   - Use `Info` for normal operations
   - Use `Warning` for potential issues
   - Use `Error` for failures
   - Use `Success` for completed operations
   - Use `Trace` for development debugging
4. **Include Context**: Add relevant details (serial numbers, timestamps, etc.)

## Example Flow

```csharp
// DataEntryTab.cs
@inject MessageConsoleService MessageConsole

private async Task DownloadUnitAsync()
{
    try
    {
        MessageConsole.AddInstruction("Enter serial number and press F1", "DataEntry");
        MessageConsole.AddInfo($"Downloading unit {serialNumber}...", "DataEntry");
        
        var unit = await TestManager.DownloadUnitAsync(serialNumber);
        
        MessageConsole.AddSuccess($"Unit {unit.SerialNumber} loaded", "DataEntry");
        MessageConsole.AddInfo($"Serial: {unit.SerialNumber}, KVA: {unit.Kva}", "DataEntry");
    }
    catch (Exception ex)
    {
        MessageConsole.AddError($"Download failed: {ex.Message}", "DataEntry");
        MessageConsole.AddTrace($"Exception: {ex.StackTrace}", "Debug");
    }
}
```
