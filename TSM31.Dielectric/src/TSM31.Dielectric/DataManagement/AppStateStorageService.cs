using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TSM31.Dielectric.Operator;
using TSM31.Dielectric.Testing;

namespace TSM31.Dielectric.DataManagement;

/// <summary>
/// Service for persisting application state to SQLite database.
/// Enables crash recovery by saving operator and unit information.
/// </summary>
public class AppStateStorageService : IAppStateStorageService
{
    private readonly AppStateDbContext _dbContext;
    private readonly ILogger<AppStateStorageService> _logger;

    public AppStateStorageService(
        AppStateDbContext dbContext,
        ILogger<AppStateStorageService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets the most recent saved session state
    /// </summary>
    public async Task<SessionState?> GetLatestSessionAsync()
    {
        try
        {
            var session = await _dbContext.SessionStates
                .OrderByDescending(s => s.LastUpdated)
                .FirstOrDefaultAsync();

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve latest session");
            return null;
        }
    }

    /// <summary>
    /// Updates the current operator in the session state
    /// </summary>
    public async Task UpdateOperatorAsync(Employee? newOperator)
    {
        try
        {
            // Get or create session
            var session = await GetOrCreateSessionAsync();

            // Update operator info
            session.UpdateFromEmployee(newOperator);
            session.LastUpdated = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Operator state persisted: {OperatorId}", newOperator?.Id ?? "null");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist operator state");
        }
    }

    /// <summary>
    /// Updates the current unit information in session state
    /// </summary>
    public async Task UpdateUnitAsync(string? serialNumber, bool hasUnit)
    {
        try
        {
            var session = await GetOrCreateSessionAsync();

            session.CurrentSerialNumber = serialNumber;
            session.HasUnit = hasUnit;
            session.LastUpdated = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Unit state persisted: {SerialNumber}", serialNumber ?? "null");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist unit state");
        }
    }

    /// <summary>
    /// Updates the current test action/screen in session state
    /// </summary>
    public async Task UpdateCurrentActionAsync(TestActions action)
    {
        try
        {
            var session = await GetOrCreateSessionAsync();

            session.CurrentAction = action;
            session.LastUpdated = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Current action persisted: {Action}", action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist current action");
        }
    }

    /// <summary>
    /// Clears all session data (e.g., after successful logout and unit clear)
    /// </summary>
    public async Task ClearSessionAsync()
    {
        try
        {
            var session = await GetOrCreateSessionAsync();

            session.OperatorId = null;
            session.OperatorName = null;
            session.CurrentSerialNumber = null;
            session.HasUnit = false;
            session.CurrentAction = TestActions.Home;
            session.LastUpdated = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Session cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear session");
        }
    }

    /// <summary>
    /// Records a telemetry event
    /// </summary>
    public async Task RecordTelemetryAsync(string eventType, string? serialNumber = null, string? operatorId = null, string? details = null)
    {
        try
        {
            var telemetryEvent = new TelemetryEvent {
                EventType = eventType,
                SerialNumber = serialNumber,
                OperatorId = operatorId,
                Details = details,
                Timestamp = DateTime.Now
            };

            _dbContext.TelemetryEvents.Add(telemetryEvent);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record telemetry event: {EventType}", eventType);
        }
    }

    private async Task<SessionState> GetOrCreateSessionAsync()
    {
        var session = await _dbContext.SessionStates
            .OrderByDescending(s => s.LastUpdated)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            session = new SessionState();
            _dbContext.SessionStates.Add(session);
            await _dbContext.SaveChangesAsync();
        }

        return session;
    }
}
