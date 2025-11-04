namespace TSM31.Core.Services;

using Core.Models;
using Microsoft.Extensions.Logging;
using Persistence;

/// <summary>
/// Orchestrates the application startup sequence including service initialization,
/// session restoration checks, and dialog display decisions.
/// Extracts complex startup logic from the layout component for better testability and maintainability.
/// </summary>
public class StartupService
{
    private const string SERVICE_FUNCTION_KEY = "FunctionKeyService";
    private const string SERVICE_TEST_MANAGER = "TestManager";

    private readonly FunctionKeyService _keyService;
    private readonly DielectricTestManager _testManager;
    private readonly InitializationStateService _initStateService;
    private readonly SessionManager _sessionHandler;
    private readonly ILogger<StartupService> _logger;

    public StartupService(
        FunctionKeyService keyService,
        DielectricTestManager testManager,
        InitializationStateService initStateService,
        SessionManager sessionHandler,
        ILogger<StartupService> logger)
    {
        _keyService = keyService;
        _testManager = testManager;
        _initStateService = initStateService;
        _sessionHandler = sessionHandler;
        _logger = logger;
    }

    /// <summary>
    /// Executes the complete startup sequence for the application.
    /// Call this once during OnAfterRenderAsync(firstRender=true).
    /// </summary>
    /// <returns>Result indicating which dialog to show and any saved session data</returns>
    public async Task<StartupResult> ExecuteStartupSequenceAsync()
    {
        _logger.LogInformation("Starting application initialization sequence...");

        // Step 1: Initialize core services
        await InitializeServicesAsync();

        _logger.LogInformation("Core services initialized");

        // Step 2: Load any saved session
        var savedSession = await _sessionHandler.GetSavedSessionAsync();

        // Step 3: Check if this is first run
        var hasBeenInitialized = await CheckInitializationStateAsync();

        // Step 4: Determine which dialog to show
        var dialogToShow = DetermineInitialDialog(hasBeenInitialized, savedSession);

        return new StartupResult(dialogToShow, savedSession);
    }

    /// <summary>
    /// Initializes all required services (FunctionKeyService, TestManager).
    /// Logs errors but doesn't fail - allows app to start even if some services fail.
    /// </summary>
    private async Task InitializeServicesAsync()
    {
        _logger.LogDebug("Initializing core services...");

        // Initialize FunctionKeyService (handles JS keyboard registration)
        await TryInitializeServiceAsync(SERVICE_FUNCTION_KEY, async () => {
            await _keyService.InitializeAsync();
        });

        _logger.LogDebug("Function key service initialized");

        // Initialize TestManager (loads options cache, restores unit data silently)
        await TryInitializeServiceAsync(SERVICE_TEST_MANAGER, async () => {
            await _testManager.InitializeAsync();
        });

        _logger.LogDebug("Test manager initialized");
    }

    /// <summary>
    /// Checks if the application has been initialized before.
    /// </summary>
    private async Task<bool> CheckInitializationStateAsync()
    {
        var hasBeenInitialized = await _initStateService.HasBeenInitializedAsync();
        _logger.LogDebug("Has been initialized: {HasBeenInitialized}", hasBeenInitialized);
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
        if (_testManager.IsOperatorLoggedIn)
        {
            LogDialogDecision($"Operator already logged in: {_testManager.CurrentOperator?.Name}");
            return DialogToShow.None;
        }

        // Has saved operator session - prompt to restore
        if (savedSession?.Operator != null)
        {
            LogDialogDecision($"Showing operator restore dialog for: {savedSession.Operator.Name}");
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
            _logger.LogError(ex, "Failed to initialize {ServiceName}", serviceName);
        }
    }

    /// <summary>
    /// Helper method for consistent dialog decision logging.
    /// </summary>
    private void LogDialogDecision(string message) => _logger.LogInformation(message);
}
