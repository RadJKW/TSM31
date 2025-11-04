using RadUtils.SQLServer;
using Microsoft.Extensions.Logging;
using TSM31.Dielectric.Console;
using TSM31.Dielectric.DataManagement;

namespace TSM31.Dielectric.Operator;

/// <summary>
/// Manages employee/operator login, logout, and session state.
/// Provides persistence for crash recovery.
/// </summary>
public class EmployeeService
{
    private readonly ILogger<EmployeeService> _logger;
    private readonly MessageConsoleService _messageConsole;
    private readonly IAppStateStorageService _stateStorage;
    private readonly EmployeeLookUp _employeeLookUp = new();

    private Employee? _currentOperator;

    public EmployeeService(
        ILogger<EmployeeService> logger,
        MessageConsoleService messageConsole,
        IAppStateStorageService stateStorage)
    {
        _logger = logger;
        _messageConsole = messageConsole;
        _stateStorage = stateStorage;
    }

    /// <summary>
    /// Currently logged-in operator
    /// </summary>
    public Employee? CurrentOperator
    {
        get => _currentOperator;
        private set {
            if (_currentOperator == value) return;

            _currentOperator = value;
            OnOperatorChanged?.Invoke();
        }
    }

    /// <summary>
    /// Event raised when operator login/logout occurs
    /// </summary>
    public event Action? OnOperatorChanged;

    /// <summary>
    /// Attempts to log in an operator with the given employee number.
    /// </summary>
    public virtual async Task<bool> LoginAsync(string employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
        {
            _messageConsole.AddError("Operator ID is required", "Login");
            return false;
        }

        var trimmedEmployeeNumber = employeeNumber.Trim();

        _logger.LogInformation("Login attempt: {EmployeeNumber}", trimmedEmployeeNumber);
        _messageConsole.AddInfo($"Attempting login with Operator ID: {trimmedEmployeeNumber}", "Login");

        try
        {
            switch (trimmedEmployeeNumber)
            {
                case "00000":
                    CurrentOperator = new Employee {
                        Id = "00000",
                        Name = "Operator Test",
                        SuperVisorId = "00000",
                        IsValid = true
                    };

                    _messageConsole.AddSuccess("Test operator logged in: Operator Test", "Login");
                    _logger.LogInformation("Test operator logged in: {EmployeeId}", trimmedEmployeeNumber);
                    await _stateStorage.UpdateOperatorAsync(CurrentOperator);
                    return true;

                case "01111":
                    CurrentOperator = new Employee {
                        Id = "1111",
                        Name = "Operator Test",
                        SuperVisorId = "1111",
                        IsValid = true
                    };

                    _messageConsole.AddSuccess("Test operator logged in: Operator Test", "Login");
                    _logger.LogInformation("Test operator logged in: {EmployeeId}", trimmedEmployeeNumber);
                    await _stateStorage.UpdateOperatorAsync(CurrentOperator);
                    return true;
            }

            var employeeInfo = await _employeeLookUp.GetEmployeeInfoAsync(trimmedEmployeeNumber);

            if (employeeInfo == null)
            {
                CurrentOperator = null;
                _messageConsole.AddError($"Operator not found: {trimmedEmployeeNumber}", "Login");
                _logger.LogWarning("Employee lookup returned null for {EmployeeNumber}", trimmedEmployeeNumber);
                return false;
            }

            var status = employeeInfo.EmployeeStatus?.ToUpperInvariant() ?? string.Empty;
            var isDatabaseDown = status.StartsWith("DATABASE SERVER NOT AVAILABLE", StringComparison.OrdinalIgnoreCase)
                || status.StartsWith("DATABASE ERROR", StringComparison.OrdinalIgnoreCase);

            if (isDatabaseDown)
            {
                CurrentOperator = new Employee {
                    Id = trimmedEmployeeNumber,
                    Name = "Database Server Error",
                    IsValid = true
                };

                _logger.LogWarning("Database server not available. Allowing offline login for {EmployeeId}", trimmedEmployeeNumber);
                _messageConsole.AddWarning("Database server is not available. Operator login allowed with restricted access.", "Login");
                await _stateStorage.UpdateOperatorAsync(CurrentOperator);

                return true;
            }

            if (!string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                CurrentOperator = null;
                _messageConsole.AddError($"Invalid Operator ID: {trimmedEmployeeNumber}. Status: {employeeInfo.EmployeeStatus}", "Login");
                _logger.LogWarning("Operator login failed for {EmployeeNumber} with status {Status}", trimmedEmployeeNumber, employeeInfo.EmployeeStatus);
                return false;
            }

            CurrentOperator = new Employee {
                Id = employeeInfo.EmployeeNumber,
                Name = employeeInfo.EmployeeName,
                SuperVisorId = employeeInfo.SupervisorNumber,
                IsValid = true
            };

            _messageConsole.AddSuccess($"Operator logged in: {CurrentOperator.Name} ({CurrentOperator.Id})", "Login");
            _logger.LogInformation("Operator logged in: {OperatorName} ({OperatorId})", CurrentOperator.Name, CurrentOperator.Id);

            await _stateStorage.UpdateOperatorAsync(CurrentOperator);
            return true;
        }
        catch (Exception ex)
        {
            CurrentOperator = null;
            _logger.LogError(ex, "Employee lookup failed for {EmployeeNumber}", trimmedEmployeeNumber);
            _messageConsole.AddError($"Operator lookup failed: {ex.Message}", "Login");
            return false;
        }
    }

    /// <summary>
    /// Restores a previously logged-in operator from saved session state.
    /// Used during session restoration after crash or restart.
    /// </summary>
    public virtual async Task<bool> RestoreOperatorAsync(Employee prevOperator)
    {
        try
        {
            CurrentOperator = prevOperator;
            _messageConsole.AddSuccess($"Operator session restored: {CurrentOperator.Name} ({CurrentOperator.Id})", "Login");
            _logger.LogInformation("Operator session restored: {OperatorId}", CurrentOperator.Id);

            // Ensure persistence is updated
            await _stateStorage.UpdateOperatorAsync(CurrentOperator);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore operator session");
            _messageConsole.AddError($"Failed to restore operator: {ex.Message}", "Login");
            return false;
        }
    }

    /// <summary>
    /// Logs out the current operator
    /// </summary>
    public virtual async Task LogoutAsync()
    {
        if (CurrentOperator != null)
        {
            _messageConsole.AddInfo($"Operator logged out: {CurrentOperator.Name}", "Login");
            _logger.LogInformation("Operator logged out: {OperatorId}", CurrentOperator.Id);
        }

        CurrentOperator = null;

        // Clear operator from persisted state (but keep unit data)
        await _stateStorage.UpdateOperatorAsync(null);
    }
}
