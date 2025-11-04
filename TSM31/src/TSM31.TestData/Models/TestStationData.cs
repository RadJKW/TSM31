namespace TSM31.TestData.Models;

/// <summary>
/// Configuration and state data for the test station.
/// </summary>
public class TestStationData
{
    public TestMode Mode { get; set; } = TestMode.SetAndRecord;
    public TestStationType StationType { get; set; } = TestStationType.NonStepDown;
    public TestPower Power { get; set; } = TestPower.PowerOff;
    public string Console { get; set; } = "TS11Dx";
    public string MachineId { get; set; } = "01x5";
    public TestType CurrentTestType { get; set; } = TestType.NotTesting;
    public int CurrentTestsRequired { get; set; }
    public int FirstInducedTime { get; set; } = 18;

    // Configuration file paths
    public string? HipotCoefficientsFile { get; set; }
    public string? InducedCoefficientsFile { get; set; }
    public string? HvVariacTimeFile { get; set; }
    public string? LvVariacTimeFile { get; set; }

    // Excel and reporting configuration
    public float ImpulseScale { get; set; }
    public string? ExcelTemplate { get; set; }
    public string? ExcelLocalDir { get; set; }
    public string? ExcelServerDir { get; set; }
    public string? FailTagLocalDir { get; set; }
    public string? FailTagServerDir { get; set; }
    public string? PrinterPath { get; set; }
    public string? BarcodeCommPort { get; set; }
}

public enum TestStationType
{
    NonStepDown = 0,
    StepDown = 1
}

public enum TestMode
{
    Auto = 0,
    Manual = 1,
    SetAndRecord = 2
}

public enum TestPower
{
    PowerOff = 0,
    PowerOn = 1
}

public enum TestType
{
    NotTesting = 0,
    FirstInduced = 1,
    H1ImpulseSignature = 2,
    H1ImpulseFull = 3,
    H2ImpulseSignature = 4,
    H2ImpulseFull = 5,
    H3Impulse = 6,
    X1ImpulseSignature = 7,
    X1ImpulseFull = 8,
    X2ImpulseSignature = 9,
    X2ImpulseFull = 10,
    SecondInduced = 12,
    SimultaneousHipot = 13,
    PrimaryHipot = 14,
    SecondaryHipot = 15,
    FourLvbHipot = 16
}
