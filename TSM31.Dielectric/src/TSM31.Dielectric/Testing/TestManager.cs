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

    /// <summary>
    /// Downloads unit data from the SQL Server TestData database by serial number.
    /// </summary>
    /// <param name="identifier">Serial number (10 digits)</param>
    /// <returns>Downloaded UnitData, or null if not found</returns>
    public async Task<UnitData?> DownloadUnitAsync(string identifier)
    {
        try
        {
            _logger.LogInformation("Downloading unit data for serial number: {SerialNumber}", identifier);

            var unit = await _dataRepository.DownloadUnitAsync(identifier);

            if (unit == null)
            {
                _logger.LogWarning("Unit {SerialNumber} not found or could not be downloaded", identifier);
                return null;
            }

            _currentUnit = unit;
            OnUnitDataChanged?.Invoke();

            _logger.LogInformation("Successfully downloaded unit {SerialNumber} with {TestCount} test(s)",
                identifier, unit.TotalTests);

            return unit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading unit {Identifier}", identifier);
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
