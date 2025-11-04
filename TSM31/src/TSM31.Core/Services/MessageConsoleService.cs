namespace TSM31.Core.Services;

/// <summary>
/// Represents a single message in the console
/// </summary>
public class ConsoleMessage
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public MessageLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Category { get; set; }

    public ConsoleMessage() {}

    public ConsoleMessage(MessageLevel level, string message, string? category = null)
    {
        Level = level;
        Message = message;
        Category = category;
    }

    public string FormattedMessage => $"[{Timestamp:HH:mm:ss}] {GetLevelPrefix()}{FormatCategory()}{Message}";

    private string GetLevelPrefix() => Level switch {
        MessageLevel.Info => "[INFO] ",
        MessageLevel.Warning => "[WARN] ",
        MessageLevel.Error => "[ERROR] ",
        MessageLevel.Success => "[OK] ",
        MessageLevel.Debug => "[DBG] ",
        MessageLevel.Trace => "[TRC] ",
        MessageLevel.Instruction => "[INSTR] ",
        _ => ""
    };

    private string FormatCategory() => string.IsNullOrEmpty(Category) ? string.Empty : $"({Category}) ";

    public string GetCssClass() => Level switch {
        MessageLevel.Error => "text-red-400",
        MessageLevel.Warning => "text-yellow-400",
        MessageLevel.Success => "text-green-400",
        MessageLevel.Debug => "text-blue-400",
        MessageLevel.Trace => "text-gray-500",
        MessageLevel.Instruction => "text-cyan-400",
        MessageLevel.Info => "text-white",
        _ => "text-gray-300"
    };
}

public enum MessageLevel
{
    Info,
    Warning,
    Error,
    Success,
    Debug,
    Trace,
    Instruction,
    Event
}

/// <summary>
/// Service for managing console messages throughout the application.
/// Receives log events from Serilog via MessageConsoleSink for UI display and operator log file persistence.
/// </summary>
public class MessageConsoleService
{
    private readonly List<ConsoleMessage> _messages = new();
    private const int MaxMessages = 1000;// Keep console memory bounded
    private readonly string _logFolderPath;
    private readonly string _currentLogFilePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private const long MaxLogFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const int MaxLogFiles = 5; // Keep last 5 log files

    private const string ProductionPath = @"C:\TestStation";
    private const string DevelopmentPath = @"C:\TestStation_TSM31";
    private const string LogFolderName = "logs";

    public event Action? OnMessagesChanged;

    public IReadOnlyList<ConsoleMessage> Messages => _messages.AsReadOnly();

    public MessageConsoleService()
    {

        // Determine log folder path based on environment
        var basePath = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
            ? DevelopmentPath
            : ProductionPath;

        _logFolderPath = Path.Combine(basePath, LogFolderName);

        // Ensure log folder exists
        if (!Directory.Exists(_logFolderPath))
        {
            Directory.CreateDirectory(_logFolderPath);
        }

        // Current log file name includes date
        var logFileName = $"tsm31_{DateTime.Now:yyyyMMdd}.log";
        _currentLogFilePath = Path.Combine(_logFolderPath, logFileName);
    }

    public void AddMessage(MessageLevel level, string message, string? category = null)
    {
        var consoleMessage = new ConsoleMessage(level, message, category);
        _messages.Add(consoleMessage);

        // Trim if exceeds max
        if (_messages.Count > MaxMessages)
        {
            _messages.RemoveRange(0, _messages.Count - MaxMessages);
        }

        // Log to file asynchronously (fire and forget)
        _ = LogToFileAsync(consoleMessage);

        OnMessagesChanged?.Invoke();
    }

    // Make helper methods internal to prevent direct usage - all logging should go through ILogger
    internal void AddInfo(string message, string? category = null)
        => AddMessage(MessageLevel.Info, message, category);

    internal void AddWarning(string message, string? category = null)
        => AddMessage(MessageLevel.Warning, message, category);

    internal void AddError(string message, string? category = null)
        => AddMessage(MessageLevel.Error, message, category);

    internal void AddSuccess(string message, string? category = null)
        => AddMessage(MessageLevel.Success, message, category);

    internal void AddDebug(string message, string? category = null)
        => AddMessage(MessageLevel.Debug, message, category);

    internal void AddTrace(string message, string? category = null)
        => AddMessage(MessageLevel.Trace, message, category);

    internal void AddInstruction(string message, string? category = null)
        => AddMessage(MessageLevel.Instruction, message, category);

    internal void AddEvent(string message, string? category = null)
        => AddMessage(MessageLevel.Event, message, category);

    public void Clear()
    {
        _messages.Clear();
        OnMessagesChanged?.Invoke();
    }

    public List<ConsoleMessage> GetMessagesByLevel(MessageLevel level)
        => _messages.Where(m => m.Level == level).ToList();

    public List<ConsoleMessage> GetMessagesByCategory(string category)
        => _messages.Where(m => m.Category == category).ToList();

    public List<ConsoleMessage> GetRecentMessages(int count)
        => _messages.TakeLast(count).ToList();

    /// <summary>
    /// Writes a message to the log file with rotation support
    /// </summary>
    private async Task LogToFileAsync(ConsoleMessage message)
    {
        await _fileLock.WaitAsync();
        try
        {
            // Check if we need to rotate the log file
            if (File.Exists(_currentLogFilePath))
            {
                var fileInfo = new FileInfo(_currentLogFilePath);
                if (fileInfo.Length > MaxLogFileSizeBytes)
                {
                    RotateLogFile();
                }
            }

            // Append the formatted message to the log file
            var logEntry = $"{message.FormattedMessage}{Environment.NewLine}";
            await File.AppendAllTextAsync(_currentLogFilePath, logEntry);
        }
        catch (Exception ex)
        {
            // Don't throw - logging failures shouldn't crash the app
            // Can't use ILogger here since MessageConsoleService is part of the logging infrastructure
            Console.Error.WriteLine($"[MessageConsoleService] Failed to write to log file: {ex.Message}");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Rotates log files when max size is reached
    /// </summary>
    private void RotateLogFile()
    {
        try
        {
            // Get all existing log files sorted by creation time (oldest first)
            var logFiles = Directory.GetFiles(_logFolderPath, "tsm31_*.log")
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.CreationTime)
                .ToList();

            // If we have max files, delete the oldest one
            if (logFiles.Count >= MaxLogFiles)
            {
                var oldestFile = logFiles.First();
                oldestFile.Delete();
            }

            // Rename current log file with timestamp
            if (File.Exists(_currentLogFilePath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var rotatedFileName = $"tsm31_{timestamp}.log";
                var rotatedFilePath = Path.Combine(_logFolderPath, rotatedFileName);
                File.Move(_currentLogFilePath, rotatedFilePath);
            }
        }
        catch (Exception ex)
        {
            // Can't use ILogger here since MessageConsoleService is part of the logging infrastructure
            Console.Error.WriteLine($"[MessageConsoleService] Failed to rotate log file: {ex.Message}");
        }
    }
}
