namespace TSM31.Core.Services;

using Microsoft.Extensions.Logging;
using Persistence;

/// <summary>
/// Handles operator session restoration logic during application startup.
/// Encapsulates the decision-making and execution of session restore vs. fresh start.
/// </summary>
public class SessionManager
{
    private readonly EmployeeService _employeeService;
    private readonly AppStateStorageService _stateStorage;
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(
        EmployeeService employeeService,
        AppStateStorageService stateStorage,
        ILogger<SessionManager> logger)
    {
        _employeeService = employeeService;
        _stateStorage = stateStorage;
        _logger = logger;
    }

    /// <summary>
    /// Restores a previous operator session from saved state.
    /// This bypasses the database lookup and directly restores the operator object.
    /// </summary>
    /// <param name="session">The saved session to restore</param>
    /// <returns>True if restoration succeeded, false if fallback to login needed</returns>
    public async Task<bool> RestoreSessionAsync(SessionState session)
    {
        if (session.Operator == null)
        {
            _logger.LogError("Cannot restore session - no operator data");
            return false;
        }

        try
        {
            var (success, message) = await _employeeService.RestoreOperatorAsync(session.Operator);

            if (success)
            {
                _logger.LogInformation("Operator restored: {Message}", message);
                return true;
            }
            else
            {
                _logger.LogError("Failed to restore operator: {Message}", message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during operator session restore");
            return false;
        }
    }

    /// <summary>
    /// Clears the operator from the current session while preserving unit data.
    /// Used when user chooses to start fresh instead of restoring.
    /// </summary>
    public async Task StartFreshSessionAsync()
    {
        try
        {
            // Clear operator while preserving unit data snapshot
            await _stateStorage.UpdateOperatorAsync(null);

            _logger.LogInformation("User chose fresh start - operator cleared from session");
        }
        catch (Exception ex)
        {
            // Log but don't fail - user can still proceed with manual login
            _logger.LogError(ex, "Failed to clear operator from session");
        }
    }

    /// <summary>
    /// Attempts to load the last saved session from storage.
    /// Returns null if no session exists or if loading fails.
    /// </summary>
    public async Task<SessionState?> GetSavedSessionAsync()
    {
        try
        {
            var session = await _stateStorage.LoadLastSessionAsync();

            if (session != null)
            {
                _logger.LogInformation("Found saved session: Operator={OperatorName}, Serial={SerialNumber}",
                    session.Operator?.Name, session.UnitData?.SerialNumber);
            }
            else
            {
                _logger.LogInformation("No saved session found");
            }

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load saved session");
            return null;
        }
    }
}
