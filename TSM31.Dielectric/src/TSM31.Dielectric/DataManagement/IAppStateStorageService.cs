using TSM31.Dielectric.Operator;
using TSM31.Dielectric.Testing;

namespace TSM31.Dielectric.DataManagement;

/// <summary>
/// Interface for persisting application state to a database.
/// Enables crash recovery by saving operator and unit information.
/// </summary>
public interface IAppStateStorageService
{
    /// <summary>
    /// Gets the most recent saved session state
    /// </summary>
    Task<SessionState?> GetLatestSessionAsync();

    /// <summary>
    /// Updates the current operator in the session state
    /// </summary>
    Task UpdateOperatorAsync(Employee? currentOperator);

    /// <summary>
    /// Updates the current unit information in session state
    /// </summary>
    Task UpdateUnitAsync(string? serialNumber, bool hasUnit);

    /// <summary>
    /// Updates the current test action/screen in session state
    /// </summary>
    Task UpdateCurrentActionAsync(TestActions action);

    /// <summary>
    /// Clears all session data (e.g., after successful logout and unit clear)
    /// </summary>
    Task ClearSessionAsync();

    /// <summary>
    /// Records a telemetry event
    /// </summary>
    Task RecordTelemetryAsync(string eventType, string? serialNumber = null, string? operatorId = null, string? details = null);
}
