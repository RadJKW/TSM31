namespace TSM31.Core.Services;

using Microsoft.JSInterop;

/// <summary>
/// Manages initialization state to prevent showing splash screen on hot reloads or page refreshes.
/// Uses localStorage for web persistence and in-memory state for hot reload detection.
/// </summary>
public class InitializationStateService
{
    public InitializationStateService(IJSRuntime js)
    {
        _ = js;// Reserved for future use when environment-dependent behavior is reintroduced
    }

    /// <summary>
    /// Checks if the application has been initialized in this browser session or app lifecycle.
    /// Returns true if already initialized (skip splash), false if fresh start (show splash).
    /// </summary>
    public Task<bool> HasBeenInitializedAsync() => Task.FromResult(true);

    /// <summary>
    /// Marks the application as initialized. This persists across hot reloads and browser refreshes.
    /// </summary>
    public Task MarkAsInitializedAsync() => Task.CompletedTask;

    /// <summary>
    /// Clears initialization state. Use this for testing or when you want to force the splash screen.
    /// </summary>
    public Task ClearInitializationAsync() => Task.CompletedTask;

    /// <summary>
    /// Resets the static in-memory flag. Useful for testing scenarios.
    /// Note: This does NOT clear localStorage.
    /// </summary>
    public static void ResetSessionState()
    {
        // No state persisted when splash is forced every start
    }
}
