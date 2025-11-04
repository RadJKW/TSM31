using TSM31.Dielectric.Operator;

namespace TSM31.Dielectric.Configuration;

/// <summary>
/// Indicates which dialog should be displayed during application startup.
/// </summary>
public enum DialogToShow
{
    /// <summary>No dialog needed - operator already logged in or restored</summary>
    None,

    /// <summary>Show splash screen (first-time initialization)</summary>
    Splash,

    /// <summary>Show regular operator login dialog</summary>
    OperatorLogin,

    /// <summary>Show operator session restoration prompt</summary>
    OperatorRestore
}

/// <summary>
/// Result of the startup orchestration process, indicating UI actions to take.
/// </summary>
/// <param name="Dialog">Which dialog to display, if any</param>
/// <param name="SavedSession">Previously saved session data (null if none exists)</param>
public record StartupResult(DialogToShow Dialog, SessionState? SavedSession);
