using TSM31.Dielectric.Operator;

namespace TSM31.Dielectric.Testing;

/// <summary>
/// Core interface for test station management.
/// All test stations must implement this interface to provide navigation,
/// operator management, and unit data management.
/// </summary>
public interface ITestManager
{
    // ========== Operator Management ==========

    /// <summary>
    /// Currently logged-in operator (null if not logged in)
    /// </summary>
    Employee? CurrentOperator { get; }

    /// <summary>
    /// Indicates whether an operator is currently logged in
    /// </summary>
    bool IsOperatorLoggedIn { get; }

    /// <summary>
    /// Event raised when operator login/logout occurs
    /// </summary>
    event Action? OnOperatorChanged;

    /// <summary>
    /// Event raised when the operator login dialog should be shown
    /// </summary>
    event Action? OnShowOperatorDialog;

    /// <summary>
    /// Logs in an operator with the given employee number
    /// </summary>
    Task<bool> LoginOperatorAsync(string employeeNumber);

    /// <summary>
    /// Logs out the current operator
    /// </summary>
    void LogoutOperator();

    /// <summary>
    /// Shows the operator login dialog
    /// </summary>
    void ShowOperatorDialog();

    // ========== Unit Data Management ==========

    /// <summary>
    /// Currently loaded unit data (null if no unit loaded)
    /// </summary>
    UnitDataBase? CurrentUnit { get; }

    /// <summary>
    /// Indicates whether a unit is currently loaded
    /// </summary>
    bool HasUnit { get; }

    /// <summary>
    /// Event raised when unit data changes (download, clear, update)
    /// </summary>
    event Action? OnUnitDataChanged;

    // ========== Navigation ==========

    /// <summary>
    /// Current test action/screen
    /// </summary>
    TestActions CurrentTestActions { get; }

    /// <summary>
    /// Event raised when navigation occurs (screen change)
    /// </summary>
    event Action? OnTestActionChanged;

    /// <summary>
    /// Navigates to a specific test action/screen
    /// </summary>
    void NavigateTo(TestActions action);

    // ========== Initialization ==========

    /// <summary>
    /// Initializes the test station manager (called once at startup)
    /// </summary>
    Task InitializeAsync();
}
