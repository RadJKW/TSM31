namespace TSM31.TestData.Models;

/// <summary>
/// Contains induced voltage test data for first and second tests.
/// </summary>
[Serializable]
public class InducedData
{
    // First induced test
    public TestStatus FirstStatus { get; set; } = new();
    public string FirstLimitText { get; set; } = string.Empty;
    public string? FirstVoltStatus { get; set; }
    public string? FirstWattStatus { get; set; }
    public int FirstTimeRequired { get; set; } = 4;
    public float FirstVoltage { get; set; }
    public float FirstPower { get; set; }
    public float FirstCurrent { get; set; }
    public int FirstTestTime { get; set; }

    // First test - three phase measurements
    public float FirstC1V { get; set; }
    public float FirstC1A { get; set; }
    public float FirstC1W { get; set; }
    public float FirstC2V { get; set; }
    public float FirstC2A { get; set; }
    public float FirstC2W { get; set; }
    public float FirstC3V { get; set; }
    public float FirstC3A { get; set; }
    public float FirstC3W { get; set; }

    // Second induced test
    public TestStatus SecondStatus { get; set; } = new();
    public string SecondLimitText { get; set; } = string.Empty;
    public string? SecondVoltStatus { get; set; }
    public string? SecondWattStatus { get; set; }
    public int SecondTimeRequired { get; set; } = 18;
    public float SecondVoltage { get; set; }
    public float SecondPower { get; set; }
    public float SecondCurrent { get; set; }
    public int SecondTestTime { get; set; }

    // Second test - three phase measurements
    public float SecondC1V { get; set; }
    public float SecondC1A { get; set; }
    public float SecondC1W { get; set; }
    public float SecondC2V { get; set; }
    public float SecondC2A { get; set; }
    public float SecondC2W { get; set; }
    public float SecondC3V { get; set; }
    public float SecondC3A { get; set; }
    public float SecondC3W { get; set; }

    // Test configuration
    public int WattLimit { get; set; }
    public int SetCondition { get; set; }
    public int VoltageRange { get; set; }
    public long CurrentRange { get; set; }
}

/// <summary>
/// Collection of induced test data for multiple test runs.
/// </summary>
[Serializable]
public class InducedTests : List<InducedData>
{
}
