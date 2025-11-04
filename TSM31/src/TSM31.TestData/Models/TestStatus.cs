namespace TSM31.TestData.Models;

/// <summary>
/// Represents the status of a test result.
/// </summary>
[Serializable]
public class TestStatus
{
    public TestStatusType Status { get; set; } = TestStatusType.NotRequired;

    public TestStatus() { }

    public TestStatus(TestStatusType initialStatus)
    {
        Status = initialStatus;
    }

    public override string ToString() => Status switch
    {
        TestStatusType.NotRequired => "Not Required",
        TestStatusType.Passed => "Passed",
        TestStatusType.Done => "Done",
        TestStatusType.Hold => "Hold For Engineering",
        TestStatusType.Failed => "Failed",
        TestStatusType.Aborted => "Aborted",
        TestStatusType.Required => "Required",
        _ => "Unknown"
    };

    /// <summary>
    /// Returns a single character flag representing the status.
    /// </summary>
    public string ToFlag() => Status switch
    {
        TestStatusType.NotRequired => "N",
        TestStatusType.Passed => "P",
        TestStatusType.Done => "D",
        TestStatusType.Hold => "H",
        TestStatusType.Failed => "F",
        TestStatusType.Aborted => "A",
        TestStatusType.Required => "R",
        _ => "U"
    };

    /// <summary>
    /// Sets the status based on a single character flag.
    /// </summary>
    public void SetStatusByFlag(string flag)
    {
        Status = flag.ToUpperInvariant() switch
        {
            "N" => TestStatusType.NotRequired,
            "P" => TestStatusType.Passed,
            "D" => TestStatusType.Done,
            "H" => TestStatusType.Hold,
            "F" => TestStatusType.Failed,
            "A" => TestStatusType.Aborted,
            "R" => TestStatusType.Required,
            _ => TestStatusType.Unknown
        };
    }
}

public enum TestStatusType
{
    NotRequired = 0,
    Passed = 1,
    Done = 2,
    Hold = 3,
    Failed = 4,
    Aborted = 5,
    Required = 6,
    Unknown = 10
}
