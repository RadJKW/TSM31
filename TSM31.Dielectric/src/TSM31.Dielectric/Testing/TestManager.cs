using Microsoft.Extensions.Logging;
using TSM31.Dielectric.Operator;

namespace TSM31.Dielectric.Testing;

/// <summary>
/// Concrete test manager implementation for dielectric transformer testing.
/// Manages unit data downloads, operator sessions, and test navigation.
/// </summary>
public class TestManager : ITestManager, IDisposable
{
    private readonly EmployeeService _employeeService;
    private readonly ITestDataRepository<UnitData> _dataRepository;
    private readonly ILogger<TestManager> _logger;
    private UnitData? _currentUnit;

    public TestManager(
        EmployeeService employeeService,
        ITestDataRepository<UnitData> dataRepository,
        ILogger<TestManager> logger)
    {
        _employeeService = employeeService;
        _dataRepository = dataRepository;
        _logger = logger;

        _employeeService.OnOperatorChanged += HandleOperatorChanged;
    }

    // ========== Operator Management ==========

    public Employee? CurrentOperator => _employeeService.CurrentOperator;

    public bool IsOperatorLoggedIn => CurrentOperator != null;

    public event Action? OnOperatorChanged;
    public event Action? OnShowOperatorDialog;

    public async Task<bool> LoginOperatorAsync(string employeeNumber)
    {
        var success = await _employeeService.LoginAsync(employeeNumber);

        if (!success)
        {
            _logger.LogWarning("Operator login failed for {EmployeeNumber}", employeeNumber);
        }

        return success;
    }

    public void LogoutOperator()
    {
        _ = _employeeService.LogoutAsync();
    }

    public void ShowOperatorDialog()
    {
        OnShowOperatorDialog?.Invoke();
    }

    private void HandleOperatorChanged()
    {
        _logger.LogDebug("Operator changed. Logged in: {IsLoggedIn}", IsOperatorLoggedIn);
        OnOperatorChanged?.Invoke();
    }

    // ========== Unit Data Management ==========

    public UnitDataBase? CurrentUnit => _currentUnit;

    public UnitData? TypedCurrentUnit => _currentUnit;

    public bool HasUnit => _currentUnit != null && _currentUnit.IsDownloaded;

    public event Action? OnUnitDataChanged;
    public event Action? OnDownloadStarted;
    public event Action<bool>? OnDownloadCompleted;
    public event Action? OnPendingSerialChanged;

    // Context-specific command handlers (set by active components)
    public Action? DataEntryF1Handler { get; set; }
    public Action? DataEntryF9Handler { get; set; }

    // Pending serial number for menu state
    internal string? PendingSerialNumber { get; private set; }

    /// <summary>
    /// Downloads unit data from the SQL Server TestData database by serial number.
    /// </summary>
    /// <param name="identifier">Serial number (10 digits)</param>
    /// <returns>Downloaded UnitData, or null if not found</returns>
    public async Task<UnitData?> DownloadUnitAsync(string identifier)
    {
        // Notify UI that download is starting
        OnDownloadStarted?.Invoke();

        try
        {
            _logger.LogInformation("Downloading unit data for serial number: {SerialNumber}", identifier);

            var unit = await _dataRepository.DownloadUnitAsync(identifier);

            if (unit == null)
            {
                _logger.LogWarning("Unit {SerialNumber} not found or could not be downloaded", identifier);
                OnDownloadCompleted?.Invoke(false);
                return null;
            }

            _currentUnit = unit;
            OnUnitDataChanged?.Invoke();

            _logger.LogInformation("Successfully downloaded unit {SerialNumber} with {TestCount} test(s)",
                identifier, unit.TotalTests);

            OnDownloadCompleted?.Invoke(true);
            return unit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading unit {Identifier}", identifier);
            OnDownloadCompleted?.Invoke(false);
            return null;
        }
    }

    /// <summary>
    /// Clears the currently loaded unit data.
    /// </summary>
    public void ClearUnit()
    {
        _currentUnit = null;
        OnUnitDataChanged?.Invoke();
        _logger.LogInformation("Unit data cleared");
    }

    // ========== Test Data Access Methods ==========

    /// <summary>
    /// Gets all hipot test data for the current unit.
    /// </summary>
    public List<HipotData>? GetHipotTestData()
    {
        return _currentUnit?.Hipot;
    }

    /// <summary>
    /// Gets all induced test data for the current unit.
    /// </summary>
    public List<InducedData>? GetInducedTestData()
    {
        return _currentUnit?.Induced;
    }

    /// <summary>
    /// Gets all impulse test data for the current unit.
    /// </summary>
    public List<ImpulseData>? GetImpulseTestData()
    {
        return _currentUnit?.Impulse;
    }

    /// <summary>
    /// Gets the hipot test data for the current test number.
    /// </summary>
    public HipotData? GetCurrentHipotTest()
    {
        if (_currentUnit == null || _currentUnit.Hipot.Count == 0)
            return null;

        var index = _currentUnit.CurrentTest - 1;
        return index >= 0 && index < _currentUnit.Hipot.Count
            ? _currentUnit.Hipot[index]
            : null;
    }

    /// <summary>
    /// Gets the induced test data for the current test number.
    /// </summary>
    public InducedData? GetCurrentInducedTest()
    {
        if (_currentUnit == null || _currentUnit.Induced.Count == 0)
            return null;

        var index = _currentUnit.CurrentTest - 1;
        return index >= 0 && index < _currentUnit.Induced.Count
            ? _currentUnit.Induced[index]
            : null;
    }

    /// <summary>
    /// Gets the impulse test data for the current test number.
    /// </summary>
    public ImpulseData? GetCurrentImpulseTest()
    {
        if (_currentUnit == null || _currentUnit.Impulse.Count == 0)
            return null;

        var index = _currentUnit.CurrentTest - 1;
        return index >= 0 && index < _currentUnit.Impulse.Count
            ? _currentUnit.Impulse[index]
            : null;
    }

    /// <summary>
    /// Gets the ratings data for the current test number.
    /// </summary>
    public DielectricRatings? GetCurrentRatings()
    {
        if (_currentUnit == null || _currentUnit.DielectricRatings.Count == 0)
            return null;

        var index = _currentUnit.CurrentTest - 1;
        return index >= 0 && index < _currentUnit.DielectricRatings.Count
            ? _currentUnit.DielectricRatings[index]
            : null;
    }

    // ========== Navigation ==========

    public TestActions CurrentTestActions { get; private set; } = TestActions.Home;

    public event Action? OnTestActionChanged;

    public void NavigateTo(TestActions action)
    {
        if (CurrentTestActions == action)
        {
            return;
        }

        CurrentTestActions = action;
        OnTestActionChanged?.Invoke();
        _logger.LogDebug("Navigated to: {Action}", action);
    }

    // ========== Multi-Test Support ==========

    /// <summary>
    /// Cycles to the next test (for units with multiple tests).
    /// Wraps around to test 1 after the last test.
    /// </summary>
    public void CycleCurrentTest()
    {
        if (_currentUnit == null)
            return;

        if (_currentUnit.TotalTests <= 1)
            return;

        _currentUnit.CurrentTest++;

        if (_currentUnit.CurrentTest > _currentUnit.TotalTests)
        {
            _currentUnit.CurrentTest = 1;
        }

        OnUnitDataChanged?.Invoke();
        _logger.LogInformation("Cycled to test {CurrentTest} of {TotalTests}",
            _currentUnit.CurrentTest, _currentUnit.TotalTests);
    }

    // ========== Validation Methods ==========

    /// <summary>
    /// Updates the pending serial number (for UI state).
    /// </summary>
    public void UpdatePendingSerial(string? serial)
    {
        PendingSerialNumber = serial;
        OnPendingSerialChanged?.Invoke();
    }

    /// <summary>
    /// Validates a serial number format.
    /// </summary>
    public bool ValidateSerialNumber(string? serialNumber)
    {
        return !string.IsNullOrWhiteSpace(serialNumber) &&
               serialNumber.All(char.IsDigit) &&
               serialNumber.Length >= 3;
    }

    /// <summary>
    /// Validates download readiness and returns status message.
    /// </summary>
    public (bool IsValid, string Message) ValidateDownload(string? serialNumber, string? catalogNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            return (false, "Please enter a serial number.");

        if (!ValidateSerialNumber(serialNumber))
            return (false, "Serial must be numeric and at least 3 digits.");

        if (_currentUnit != null && string.Equals(_currentUnit.SerialNumber, serialNumber, StringComparison.Ordinal))
            return (true, "Ready to re-download (will refresh data).");

        if (_currentUnit != null)
            return (true, "Ready to download (will replace current unit).");

        return (true, "Ready to download.");
    }

    /// <summary>
    /// Gets primary BIL options for UI dropdowns (placeholder - would load from database).
    /// </summary>
    public IEnumerable<string> GetPrimaryBILOptions()
    {
        return new[] { "025", "045", "060", "075", "095", "110", "125", "150", "200", "250" };
    }

    /// <summary>
    /// Gets secondary BIL options for UI dropdowns (placeholder - would load from database).
    /// </summary>
    public IEnumerable<string> GetSecondaryBILOptions()
    {
        return new[] { "010", "020", "030", "045", "060" };
    }

    // ========== Initialization ==========

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing dielectric test station manager");
        await Task.CompletedTask;
    }

    // ========== Disposal ==========

    public void Dispose()
    {
        _employeeService.OnOperatorChanged -= HandleOperatorChanged;
    }
}
