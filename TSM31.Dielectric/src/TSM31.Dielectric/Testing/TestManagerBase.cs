using Microsoft.Extensions.Logging;
using TSM31.Dielectric.Operator;

namespace TSM31.Dielectric.Testing;

/// <summary>
/// Abstract base class providing common test station management functionality.
/// Test-station-specific implementations should extend this class.
/// </summary>
/// <typeparam name="TUnitData">The test-station-specific unit data type</typeparam>
public abstract class TestManagerBase<TUnitData> : ITestManager
    where TUnitData : UnitDataBase
{
    protected readonly EmployeeService EmployeeService;
    protected readonly ILogger Logger;

    protected TestManagerBase(
        EmployeeService employeeService,
        ILogger logger)
    {
        EmployeeService = employeeService;
        Logger = logger;

        // Subscribe to employee service events
        EmployeeService.OnOperatorChanged += HandleOperatorChanged;
    }

    // ========== Operator Management ==========

    public Employee? CurrentOperator { get; protected set; }

    public bool IsOperatorLoggedIn => CurrentOperator != null;

    public event Action? OnOperatorChanged;
    public event Action? OnShowOperatorDialog;

    public virtual async Task<bool> LoginOperatorAsync(string employeeNumber)
    {
        try
        {
            var success = await EmployeeService.LoginAsync(employeeNumber);

            if (success)
            {
                CurrentOperator = EmployeeService.CurrentOperator;
                OnOperatorChanged?.Invoke();
                Logger.LogInformation("Operator logged in: {OperatorName}", CurrentOperator?.Name);
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error logging in operator");
            return false;
        }
    }

    public virtual void LogoutOperator()
    {
        _ = EmployeeService.LogoutAsync();
        CurrentOperator = null;
        OnOperatorChanged?.Invoke();
        Logger.LogInformation("Operator logged out");
    }

    public virtual void ShowOperatorDialog()
    {
        OnShowOperatorDialog?.Invoke();
    }

    private void HandleOperatorChanged()
    {
        CurrentOperator = EmployeeService.CurrentOperator;
        OnOperatorChanged?.Invoke();
    }

    // ========== Unit Data Management ==========

    public TUnitData? TypedCurrentUnit { get; protected set; }

    public UnitDataBase? CurrentUnit => TypedCurrentUnit;

    public bool HasUnit => TypedCurrentUnit != null && TypedCurrentUnit.IsDownloaded;

    public event Action? OnUnitDataChanged;

    /// <summary>
    /// Downloads/loads unit data. Implement test-station-specific download logic.
    /// </summary>
    public abstract Task<TUnitData?> DownloadUnitAsync(string identifier);

    /// <summary>
    /// Clears the currently loaded unit data
    /// </summary>
    public virtual void ClearUnit()
    {
        TypedCurrentUnit = null;
        OnUnitDataChanged?.Invoke();
        Logger.LogInformation("Unit data cleared");
    }

    protected virtual void NotifyUnitDataChanged()
    {
        OnUnitDataChanged?.Invoke();
    }

    // ========== Navigation ==========

    public TestActions CurrentTestActions { get; protected set; } = TestActions.Home;

    public event Action? OnTestActionChanged;

    public virtual void NavigateTo(TestActions action)
    {
        if (CurrentTestActions == action)
            return;

        CurrentTestActions = action;
        OnTestActionChanged?.Invoke();
        Logger.LogDebug("Navigated to: {Action}", action);
    }

    // ========== Initialization ==========

    public virtual async Task InitializeAsync()
    {
        Logger.LogInformation("Initializing test station manager...");
        await Task.CompletedTask;
    }

    // ========== Multi-Test Support ==========

    /// <summary>
    /// Cycles to the next test (for units with multiple tests)
    /// </summary>
    public virtual void CycleCurrentTest()
    {
        if (TypedCurrentUnit == null)
            return;

        if (TypedCurrentUnit.TotalTests <= 1)
            return;

        TypedCurrentUnit.CurrentTest++;

        if (TypedCurrentUnit.CurrentTest > TypedCurrentUnit.TotalTests)
        {
            TypedCurrentUnit.CurrentTest = 1;
        }

        OnUnitDataChanged?.Invoke();
        Logger.LogInformation("Cycled to test {CurrentTest} of {TotalTests}",
            TypedCurrentUnit.CurrentTest, TypedCurrentUnit.TotalTests);
    }
}
