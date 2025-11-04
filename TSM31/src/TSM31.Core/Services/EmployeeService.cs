namespace TSM31.Core.Services;

using Core.Models;
using Microsoft.Extensions.Logging;
using Persistence;
using RadUtils.SQLServer;
using Utils;

/*
 * Manages the Employee logged in to the system.
 * Implement Login, Logout with SQLite persistence for crash recovery.
 * Keeps State so the Employee can be accessed from anywhere in the system.
 *
 */
public class EmployeeService
{
    private readonly ILogger<EmployeeService> _logger;
    private readonly MessageConsoleService _messageConsole;
    private readonly AppStateStorageService _stateStorage;

    private readonly EmployeeLookUp _employeeLookUp = new();

    private Employee? _operator;

    public EmployeeService(
        ILogger<EmployeeService> logger,
        MessageConsoleService messageConsole,
        AppStateStorageService stateStorage)
    {
        _logger = logger;
        _messageConsole = messageConsole;
        _stateStorage = stateStorage;
    }

    public Employee? Operator
    {
        get => _operator;
        private set {
            if (_operator == value) return;

            _operator = value;
            OnOperatorChanged?.Invoke();
            // only invoke this if there is a change from null to not null or vice versa
        }
    }

    public event Action? OnOperatorChanged;

    public async Task<(bool Success, string Message)> LoginAsync(string employeeNumber)
    {
        _messageConsole.AddInfo($"Attempting login with Operator ID: {employeeNumber}", "Login");

        // Handle test operators
        switch (employeeNumber)
        {
            case "00000":
                Operator = new Employee {
                    Id = "00000",
                    Name = "Operator Test",
                    SuperVisorId = "00000",
                    IsValid = true
                };
                _messageConsole.AddSuccess("Test operator logged in: Operator Test", "Login");
                // Persist operator state to SQLite (must complete before returning)
                await _stateStorage.UpdateOperatorAsync(Operator);
                return (true, "Welcome, Operator Test!");
            case "01111":
                Operator = new Employee {
                    Id = "1111",
                    Name = "Operator Test",
                    SuperVisorId = "1111",
                    IsValid = true
                };
                _messageConsole.AddSuccess("Test operator logged in: Operator Test", "Login");
                // Persist operator state to SQLite (must complete before returning)
                await _stateStorage.UpdateOperatorAsync(Operator);
                return (true, "Welcome, Operator Test!");
        }

        // Lookup employee via RadUtils.SqlMethods asynchronously
        var employeeInfo = await _employeeLookUp.GetEmployeeInfoAsync(employeeNumber);

        var status = employeeInfo.EmployeeStatus.ToUpper();
        var isDatabaseDown = status.StartsWith("DATABASE SERVER NOT AVAILABLE") || status.StartsWith("DATABASE ERROR");

        if (isDatabaseDown)
        {
            Operator = new Employee {
                Id = employeeNumber,
                Name = "Database Server Error",
                IsValid = true
            };
            _logger.LogWarning("Database server is not available.");
            _messageConsole.AddWarning("Database server is not available. Operator login allowed with restricted access.", "Login");
            _logger.LogInformation("OperatorID: {OperatorId} logged in (offline mode)", Operator.Id);
            _logger.LogDebug("Operator details: {OperatorJson}", Operator.ToJsonPretty());

            // Persist operator state to SQLite (must complete before returning)
            await _stateStorage.UpdateOperatorAsync(Operator);

            return (true, "Database offline - limited access granted");
        }

        // Validate employee status
        if (status != "ACTIVE")
        {
            Operator = null;
            _logger.LogError("Employee {EmployeeNumber} status: {Status}", employeeNumber, employeeInfo.EmployeeStatus);
            _messageConsole.AddError($"Invalid Operator ID: {employeeNumber}. Status: {employeeInfo.EmployeeStatus}", "Login");
            return (false, $"Invalid status: {employeeInfo.EmployeeStatus}");
        }

        Operator = new Employee {
            Id = employeeInfo.EmployeeNumber,
            Name = employeeInfo.EmployeeName,
            SuperVisorId = employeeInfo.SupervisorNumber,
            IsValid = true
        };
        _messageConsole.AddSuccess($"Operator logged in: {Operator.Name} ({Operator.Id})", "Login");
        _logger.LogInformation("OperatorID: {OperatorId} logged in.", Operator.Id);
        Console.WriteLine(Operator.ToJsonPretty());

        // Persist operator state to SQLite for crash recovery (must complete before returning)
        await _stateStorage.UpdateOperatorAsync(Operator);

        return (true, $"Welcome, {Operator.Name}!");
    }

    /// <summary>
    /// Restores a previously logged-in operator from saved session state.
    /// Used during session restoration after crash or restart.
    /// </summary>
    public async Task<(bool Success, string Message)> RestoreOperatorAsync(Employee prevOperator)
    {
        try
        {
            Operator = prevOperator;
            _messageConsole.AddSuccess($"Operator session restored: {Operator.Name} ({Operator.Id})", "Login");
            _logger.LogInformation("OperatorID: {OperatorId} restored from session.", Operator.Id);

            // Ensure persistence is updated (must complete before returning)
            await _stateStorage.UpdateOperatorAsync(Operator);

            return (true, $"Welcome back, {Operator.Name}!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore operator session");
            _messageConsole.AddError($"Failed to restore operator: {ex.Message}", "Login");
            return (false, "Failed to restore operator session");
        }
    }

    public async Task LogoutAsync()
    {
        if (Operator != null)
        {
            _messageConsole.AddInfo($"Operator logged out: {Operator.Name}", "Login");
        }
        Operator = null;
        _logger.LogInformation("Operator logged out.");

        // Clear operator from persisted state (but keep unit data) - must complete before returning
        await _stateStorage.UpdateOperatorAsync(null);
    }

    /// <summary>
    /// Legacy synchronous logout method for backward compatibility.
    /// Consider using LogoutAsync instead.
    /// </summary>
    public void Logout()
    {
        _ = LogoutAsync();// Fire and forget
    }
}
