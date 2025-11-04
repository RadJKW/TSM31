using Microsoft.Extensions.Logging;
using TSM31.Dielectric.DataManagement;

namespace TSM31.Dielectric.Operator;

/// <summary>
/// Manages application state persistence and restoration for crash recovery.
/// Handles restoring the full application state including operator, unit data, and current screen
/// after crashes, restarts, or power loss.
/// </summary>
public class SessionManager
{
    private readonly EmployeeService _employeeService;
    private readonly IAppStateStorageService _stateStorage;
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(
        EmployeeService employeeService,
        IAppStateStorageService stateStorage,
        ILogger<SessionManager> logger)
    {
        _employeeService = employeeService;
        _stateStorage = stateStorage;
        _logger = logger;
    }

    /// <summary>
    /// Restores the application state from a previously saved session.
    /// This includes operator login state, unit data, and screen position.
    /// </summary>
    /// <param name="session">The saved session to restore</param>
    /// <returns>True if restoration succeeded, false if fallback to login needed</returns>
    public async Task<bool> RestoreSessionAsync(SessionState session)
    {
        var employee = session.ToEmployee();

        if (employee == null)
        {
            _logger.LogError("Cannot restore session - no operator data");
            return false;
        }

        try
        {
            var success = await _employeeService.RestoreOperatorAsync(employee);

            if (success)
            {
                _logger.LogInformation("Operator restored: {OperatorName}", employee.Name);
                return true;
            }
            else
            {
                _logger.LogError("Failed to restore operator");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during application session restore");
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
            // Clear operator while preserving unit data
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
    /// Attempts to load the last saved application session from storage.
    /// Returns null if no session exists or if loading fails.
    /// </summary>
    public async Task<SessionState?> GetSavedSessionAsync()
    {
        try
        {
            var session = await _stateStorage.GetLatestSessionAsync();

            if (session != null)
            {
                _logger.LogInformation("Found saved session: Operator={OperatorName}, Serial={SerialNumber}",
                session.OperatorName, session.CurrentSerialNumber);
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
