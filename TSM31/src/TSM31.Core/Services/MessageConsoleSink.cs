namespace TSM31.Core.Services;

using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Serilog sink that routes log events to MessageConsoleService for UI display.
/// Supports custom message types via UIMessageType property (Success, Instruction, Event).
/// </summary>
public class MessageConsoleSink : ILogEventSink
{
    private readonly MessageConsoleService _consoleService;
    private readonly IFormatProvider? _formatProvider;

    public MessageConsoleSink(MessageConsoleService consoleService, IFormatProvider? formatProvider = null)
    {
        _consoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        // Determine message level (standard or custom)
        var messageLevel = GetMessageLevel(logEvent);

        // Extract category from SourceContext (e.g., "TSM31.Core.Services.DielectricTestManager" → "DielectricTestManager")
        var category = ExtractCategory(logEvent);

        // Render structured message template
        var message = logEvent.RenderMessage(_formatProvider);

        // Route to MessageConsoleService for UI display and file logging
        _consoleService.AddMessage(messageLevel, message, category);
    }

    private MessageLevel GetMessageLevel(LogEvent logEvent)
    {
        // Check for custom UIMessageType property first
        if (logEvent.Properties.TryGetValue("UIMessageType", out var uiTypeProp))
        {
            var uiType = uiTypeProp.ToString().Trim('"');
            return uiType switch
            {
                "Success" => MessageLevel.Success,
                "Instruction" => MessageLevel.Instruction,
                "Event" => MessageLevel.Event,
                _ => MapStandardLevel(logEvent.Level)
            };
        }

        // Default to standard level mapping
        return MapStandardLevel(logEvent.Level);
    }

    private MessageLevel MapStandardLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => MessageLevel.Trace,
            LogEventLevel.Debug => MessageLevel.Debug,
            LogEventLevel.Information => MessageLevel.Info,
            LogEventLevel.Warning => MessageLevel.Warning,
            LogEventLevel.Error => MessageLevel.Error,
            LogEventLevel.Fatal => MessageLevel.Error, // Map Fatal to Error (can add styling in UI later)
            _ => MessageLevel.Info
        };
    }

    private string? ExtractCategory(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            var fullName = sourceContext.ToString().Trim('"');
            // Extract last segment (e.g., "TSM31.Core.Services.SessionManager" → "SessionManager")
            return fullName.Split('.').Last();
        }
        return null;
    }
}
