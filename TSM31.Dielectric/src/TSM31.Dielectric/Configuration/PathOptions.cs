namespace TSM31.Dielectric.Configuration;

/// <summary>
/// Configuration options for application file system paths.
/// Provides centralized path management for database, logs, and other file system operations.
/// </summary>
public class PathOptions
{
    /// <summary>
    /// Base directory for all application data.
    /// Production: C:\TSM31.Dielectric
    /// Development: C:\TSM31.Dielectric-Dev
    /// </summary>
    public string BaseDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Subfolder for database files (relative to BaseDirectory).
    /// Default: "Database"
    /// </summary>
    public string DatabaseSubfolder { get; set; } = "Database";

    /// <summary>
    /// Subfolder for log files (relative to BaseDirectory).
    /// Default: "Logs"
    /// </summary>
    public string LogsSubfolder { get; set; } = "Logs";

    /// <summary>
    /// Full path to the database directory.
    /// </summary>
    public string DatabaseDirectory => Path.Combine(BaseDirectory, DatabaseSubfolder);

    /// <summary>
    /// Full path to the logs directory.
    /// </summary>
    public string LogsDirectory => Path.Combine(BaseDirectory, LogsSubfolder);
}
