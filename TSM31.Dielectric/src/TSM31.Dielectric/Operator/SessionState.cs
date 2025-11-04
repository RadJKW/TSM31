using TSM31.Dielectric.Testing;

namespace TSM31.Dielectric.Operator;

/// <summary>
/// Represents a persisted session state for crash recovery.
/// </summary>
public class SessionState
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when session was last saved
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    /// <summary>
    /// Logged-in operator (if any)
    /// </summary>
    public Employee? Operator { get; set; }

    /// <summary>
    /// Operator ID (for persistence)
    /// </summary>
    public string? OperatorId { get; set; }

    /// <summary>
    /// Operator name (for persistence)
    /// </summary>
    public string? OperatorName { get; set; }

    /// <summary>
    /// Currently loaded unit serial number (if any)
    /// </summary>
    public string? CurrentSerialNumber { get; set; }

    /// <summary>
    /// Current test action/screen
    /// </summary>
    public TestActions CurrentAction { get; set; } = TestActions.Home;

    /// <summary>
    /// Indicates if unit data was downloaded
    /// </summary>
    public bool HasUnit { get; set; }

    /// <summary>
    /// Converts stored operator data back to Employee object
    /// </summary>
    public Employee? ToEmployee()
    {
        if (string.IsNullOrEmpty(OperatorId) || string.IsNullOrEmpty(OperatorName))
            return null;

        return new Employee {
            Id = OperatorId,
            Name = OperatorName,
            IsValid = true
        };
    }

    /// <summary>
    /// Updates session from an Employee object
    /// </summary>
    public void UpdateFromEmployee(Employee? employee)
    {
        if (employee == null)
        {
            OperatorId = null;
            OperatorName = null;
        }
        else
        {
            OperatorId = employee.Id;
            OperatorName = employee.Name;
        }
    }
}
