namespace TSM31.Dielectric.Testing;

/// <summary>
/// Abstract base class for test results.
/// Test-station-specific implementations should extend this with additional measurements and data.
/// </summary>
public abstract class TestResultBase
{
    /// <summary>
    /// Unique identifier for this test result
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Serial number of the tested unit
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Test number (for multi-test units)
    /// </summary>
    public int TestNumber { get; set; } = 1;

    /// <summary>
    /// Timestamp when the test was performed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Employee/operator ID who performed the test
    /// </summary>
    public string OperatorId { get; set; } = string.Empty;

    /// <summary>
    /// Test status (Passed, Failed, Aborted, etc.)
    /// </summary>
    public TestResultStatus Status { get; set; } = TestResultStatus.NotStarted;

    /// <summary>
    /// Optional notes or remarks about the test
    /// </summary>
    public string? Remarks { get; set; }
}

/// <summary>
/// Standard test status values for test results
/// </summary>
public enum TestResultStatus
{
    NotStarted,
    InProgress,
    Passed,
    Failed,
    Aborted,
    NotRequired
}
