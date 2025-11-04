namespace TSM31.TestData.Models;

/// <summary>
/// Contains impulse test data for H1, H2, H3, X1, X2, and X3 bushings.
/// </summary>
[Serializable]
public class ImpulseData
{
    // H1 bushing impulse test
    public int H1ShotCounter { get; set; }
    public TestStatus H1Status { get; set; } = new();
    public TestStatus H1WaveformCompare { get; set; } = new();
    public float H1Voltage { get; set; }

    // H2 bushing impulse test
    public int H2ShotCounter { get; set; }
    public TestStatus H2Status { get; set; } = new();
    public TestStatus H2WaveformCompare { get; set; } = new();
    public float H2Voltage { get; set; }

    // H3 bushing impulse test
    public int H3ShotCounter { get; set; }
    public TestStatus H3Status { get; set; } = new();
    public TestStatus H3WaveformCompare { get; set; } = new();
    public float H3Voltage { get; set; }

    // X1 bushing impulse test
    public int X1ShotCounter { get; set; }
    public TestStatus X1Status { get; set; } = new();
    public TestStatus X1WaveformCompare { get; set; } = new();
    public float X1Voltage { get; set; }

    // X2 bushing impulse test
    public int X2ShotCounter { get; set; }
    public TestStatus X2Status { get; set; } = new();
    public TestStatus X2WaveformCompare { get; set; } = new();
    public float X2Voltage { get; set; }

    // X3 bushing impulse test
    public int X3ShotCounter { get; set; }
    public TestStatus X3Status { get; set; } = new();
    public TestStatus X3WaveformCompare { get; set; } = new();
    public float X3Voltage { get; set; }

    // Test configuration
    public float SetCondition { get; set; }
    public float SecondarySetCondition { get; set; }
}

/// <summary>
/// Collection of impulse test data for multiple test runs.
/// </summary>
[Serializable]
public class ImpulseTests : List<ImpulseData>
{
}
