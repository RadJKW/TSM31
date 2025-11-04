using Serilog.Core;
using Serilog.Events;

namespace TSM31.Dielectric.Console;

/// <summary>
/// Serilog sink that routes log events to MessageConsoleService for UI display.
/// Supports custom message types via UIMessageType property (Success, Instruction, Event).
/// Filters out infrastructure logs that aren't relevant to operators.
/// </summary>
public class MessageConsoleSink(MessageConsoleService consoleService, IFormatProvider? formatProvider = null) : ILogEventSink
{
    private readonly MessageConsoleService _consoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));

    // Infrastructure namespaces and categories to exclude from operator console/logs
    private readonly static string[] ExcludedNamespaces = [
        "Microsoft.Hosting.Lifetime",
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Jering.Javascript.NodeJS",
        "System.Net.Http",
        "Microsoft.Extensions.Hosting"
    ];

    // Categories that appear in logs but should be filtered (partial match)
    private readonly static string[] ExcludedCategories = [
        "NodeServices",
        "Lifetime"
    ];

    public void Emit(LogEvent logEvent)
    {
        // Filter out infrastructure logs
        if (ShouldExclude(logEvent))
        {
            return;
        }

        // Determine message level (standard or custom)
        var messageLevel = GetMessageLevel(logEvent);

        // Extract category from SourceContext (e.g., "TSM31.Dielectric.Core.Services.SessionManager" → "SessionManager")
        var category = ExtractCategory(logEvent);

        // Render structured message template
        var message = logEvent.RenderMessage(formatProvider);

        // Route to MessageConsoleService for UI display and file logging
        _consoleService.AddMessage(messageLevel, message, category);
    }

    private bool ShouldExclude(LogEvent logEvent)
    {
        // Always include events with custom UIMessageType (explicitly marked for operator display)
        if (logEvent.Properties.ContainsKey("UIMessageType"))
        {
            return false;
        }

        // Exclude Debug and Trace level messages (too detailed for operators)
        if (logEvent.Level == LogEventLevel.Debug || logEvent.Level == LogEventLevel.Verbose)
        {
            return true;
        }

        // Check if source context is in excluded namespaces
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            var fullName = sourceContext.ToString().Trim('"');

            // Check full namespace exclusions
            if (ExcludedNamespaces.Any(ns => fullName.StartsWith(ns, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        // Extract category and check if it should be excluded
        var category = ExtractCategory(logEvent);
        if (!string.IsNullOrEmpty(category) &&
            ExcludedCategories.Any(cat => category.Contains(cat, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }
    private MessageLevel GetMessageLevel(LogEvent logEvent)
    {
        // Check for custom UIMessageType property first
        if (logEvent.Properties.TryGetValue("UIMessageType", out var uiTypeProp))
        {
            var uiType = uiTypeProp.ToString().Trim('"');
            return uiType switch {
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
        return level switch {
            LogEventLevel.Verbose => MessageLevel.Trace,
            LogEventLevel.Debug => MessageLevel.Debug,
            LogEventLevel.Information => MessageLevel.Info,
            LogEventLevel.Warning => MessageLevel.Warning,
            LogEventLevel.Error => MessageLevel.Error,
            LogEventLevel.Fatal => MessageLevel.Error,// Map Fatal to Error (can add styling in UI later)
            _ => MessageLevel.Info
        };
    }

    private string? ExtractCategory(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            var fullName = sourceContext.ToString().Trim('"');
            // Extract last segment (e.g., "TSM31.Dielectric.Core.Services.SessionManager" → "SessionManager")
            return fullName.Split('.').Last();
        }
        return null;
    }
}
