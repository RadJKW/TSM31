using Microsoft.Extensions.Logging;
using TSM31.Dielectric.Operator;

namespace TSM31.Dielectric.Testing;

public class TestManager : ITestManager, IDisposable
{
    private readonly EmployeeService _employeeService;
    private readonly ILogger<TestManager> _logger;

    public TestManager(EmployeeService employeeService, ILogger<TestManager> logger)
    {
        _employeeService = employeeService;
        _logger = logger;

        _employeeService.OnOperatorChanged += HandleOperatorChanged;
    }

    public Employee? CurrentOperator => _employeeService.CurrentOperator;

    public bool IsOperatorLoggedIn => CurrentOperator != null;

    public event Action? OnOperatorChanged;
    public event Action? OnShowOperatorDialog;

    public UnitDataBase? CurrentUnit => null;

    public bool HasUnit => false;

    public event Action? OnUnitDataChanged;

    public TestActions CurrentTestActions { get; private set; } = TestActions.Home;

    public event Action? OnTestActionChanged;

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

    public void NavigateTo(TestActions action)
    {
        if (CurrentTestActions == action)
        {
            return;
        }

        CurrentTestActions = action;
        OnTestActionChanged?.Invoke();
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing test station manager");
        await Task.CompletedTask;
    }

    private void HandleOperatorChanged()
    {
        _logger.LogDebug("Operator changed. Logged in: {IsLoggedIn}", IsOperatorLoggedIn);
        OnOperatorChanged?.Invoke();
    }

    public void Dispose()
    {
        _employeeService.OnOperatorChanged -= HandleOperatorChanged;
    }
}
