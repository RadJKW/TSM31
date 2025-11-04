namespace TSM31.Dielectric.Configuration;

/// <summary>
/// Will be used to validate communication to IO and Measurement equipment during app startup.
///  </summary>
public class InitializationStateService
{

    private bool _hasBeenInitialized;

    /// <summary>
    /// Checks if the application has been initialized in this browser session or app lifecycle.
    /// Returns true if already initialized (skip splash), false if fresh start (show splash).
    /// </summary>
    public Task<bool> HasBeenInitializedAsync()
    {
        return Task.FromResult(_hasBeenInitialized);
    }

    /// <summary>
    /// Marks the application as initialized. This persists across hot reloads and browser refreshes.
    /// </summary>
    public Task MarkAsInitializedAsync()
    {
        _hasBeenInitialized = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears initialization state. Use this for testing or when you want to force the splash screen.
    /// </summary>
    public Task ClearInitializationAsync()
    {
        _hasBeenInitialized = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets the static in-memory flag. Useful for testing scenarios.
    /// </summary>
    public void ResetSessionState()
    {
        _hasBeenInitialized = false;
    }
}
