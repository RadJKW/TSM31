using Microsoft.Extensions.Logging;
using TSM31.Dielectric.Navigation;
using TSM31.Dielectric.Operator;
using TSM31.Dielectric.Testing;

namespace TSM31.Dielectric.Configuration;

/// <summary>
/// Orchestrates the application startup sequence including service initialization,
/// session restoration checks, and dialog display decisions.
/// Extracts complex startup logic from the layout component for better testability and maintainability.
/// </summary>
public class StartupService(
    FunctionKeyService keyService,
    ITestManager testManager,
    InitializationStateService initStateService,
    SessionManager sessionHandler,
    ILogger<StartupService> logger)
{
    private const string SERVICE_FUNCTION_KEY = "FunctionKeyService";
    private const string SERVICE_TEST_MANAGER = "TestManager";

    /// <summary>
    /// Executes the complete startup sequence for the application.
    /// Call this once during OnAfterRenderAsync(firstRender=true).
    /// </summary>
    /// <returns>Result indicating which dialog to show and any saved session data</returns>
    public async Task<StartupResult> ExecuteStartupSequenceAsync()
    {
        logger.LogInformation("Starting application initialization sequence...");

        // Step 1: Initialize core services
        await InitializeServicesAsync();

        logger.LogInformation("Core services initialized");

        // Step 2: Load any saved session
        var savedSession = await sessionHandler.GetSavedSessionAsync();

        // Step 3: Check if this is first run
        var hasBeenInitialized = await CheckInitializationStateAsync();

        // Step 4: Determine which dialog to show
        var dialogToShow = DetermineInitialDialog(hasBeenInitialized, savedSession);

        // Step 5: Mark as initialized
        await initStateService.MarkAsInitializedAsync();

        return new StartupResult(dialogToShow, savedSession);
    }

    /// <summary>
    /// Initializes all required services (FunctionKeyService, TestManager).
    /// Logs errors but doesn't fail - allows app to start even if some services fail.
    /// </summary>
    private async Task InitializeServicesAsync()
    {
        logger.LogDebug("Initializing core services...");

        // Initialize FunctionKeyService (handles JS keyboard registration)
        await TryInitializeServiceAsync(SERVICE_FUNCTION_KEY, async () => {
            await keyService.InitializeAsync();
        });

        logger.LogDebug("Function key service initialized");

        // Initialize TestManager (loads options cache, restores unit data silently)
        await TryInitializeServiceAsync(SERVICE_TEST_MANAGER, async () => {
            await testManager.InitializeAsync();
        });

        logger.LogDebug("Test station manager initialized");
    }

    /// <summary>
    /// Checks if the application has been initialized before.
    /// </summary>
    private async Task<bool> CheckInitializationStateAsync()
    {
        var hasBeenInitialized = await initStateService.HasBeenInitializedAsync();
        logger.LogDebug("Has been initialized: {HasBeenInitialized}", hasBeenInitialized);
        return hasBeenInitialized;
    }

    /// <summary>
    /// Determines which dialog should be shown based on initialization state and saved session.
    /// </summary>
    /// <param name="hasBeenInitialized">True if app has been run before</param>
    /// <param name="savedSession">Previously saved session (null if none)</param>
    /// <returns>Dialog to display</returns>
    private DialogToShow DetermineInitialDialog(bool hasBeenInitialized, SessionState? savedSession)
    {
        // First run - always show splash
        if (!hasBeenInitialized)
        {
            LogDialogDecision("Showing splash screen (first run)");
            return DialogToShow.Splash;
        }

        // Already logged in - no dialog needed
        if (testManager.IsOperatorLoggedIn)
        {
            LogDialogDecision($"Operator already logged in: {testManager.CurrentOperator?.Name}");
            return DialogToShow.None;
        }

        // Has saved operator session - prompt to restore
        var savedOperator = savedSession?.ToEmployee();
        if (savedOperator != null)
        {
            LogDialogDecision($"Showing operator restore dialog for: {savedOperator.Name}");
            return DialogToShow.OperatorRestore;
        }

        // Default - show regular login
        LogDialogDecision("Showing regular login dialog (no saved operator)");
        return DialogToShow.OperatorLogin;
    }

    /// <summary>
    /// Helper method to initialize a service with consistent error handling.
    /// </summary>
    private async Task TryInitializeServiceAsync(string serviceName, Func<Task> initAction)
    {
        try
        {
            await initAction();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize {ServiceName}", serviceName);
        }
    }

    /// <summary>
    /// Helper method for consistent dialog decision logging.
    /// </summary>
    private void LogDialogDecision(string message) => logger.LogInformation(message);
}
