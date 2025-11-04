namespace TSM31.Core.Services;

using Serilog;
using Serilog.Configuration;
using Serilog.Events;

/// <summary>
/// Extension methods for configuring Serilog with custom sinks.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Writes log events to MessageConsoleService for UI display.
    /// </summary>
    /// <param name="sinkConfiguration">The sink configuration.</param>
    /// <param name="consoleService">The MessageConsoleService to route logs to.</param>
    /// <param name="restrictedToMinimumLevel">The minimum log level to display in UI.</param>
    /// <param name="formatProvider">Optional format provider for message rendering.</param>
    /// <returns>Logger configuration for method chaining.</returns>
    public static LoggerConfiguration MessageConsoleSink(
        this LoggerSinkConfiguration sinkConfiguration,
        MessageConsoleService consoleService,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information,
        IFormatProvider? formatProvider = null)
    {
        return sinkConfiguration.Sink(
            new MessageConsoleSink(consoleService, formatProvider),
            restrictedToMinimumLevel
        );
    }
}
