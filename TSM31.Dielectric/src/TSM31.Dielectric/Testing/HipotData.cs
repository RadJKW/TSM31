namespace TSM31.Dielectric.Testing;

/// <summary>
/// Contains hipot test data for primary, secondary, and 4LVB tests.
/// </summary>
[Serializable]
public class HipotData
{
    // Primary hipot test data
    public TestStatus PrimaryStatus { get; set; } = new();
    public string PrimaryLimitText { get; set; } = string.Empty;
    public string? PrimaryVoltStatus { get; set; }
    public string? PrimaryAmpStatus { get; set; }
    public string? PrimaryTimeStatus { get; set; }
    public int PrimaryLimit { get; set; } = 100;
    public float PrimarySetCondition { get; set; }
    public float PrimaryKv { get; set; }
    public float PrimaryCurrent { get; set; }
    public int PrimaryTimeRequired { get; set; } = 60;
    public int PrimaryTestTime { get; set; }

    // Secondary hipot test data
    public TestStatus SecondaryStatus { get; set; } = new();
    public string SecondaryLimitText { get; set; } = string.Empty;
    public string? SecondaryVoltStatus { get; set; }
    public string? SecondaryAmpStatus { get; set; }
    public string? SecondaryTimeStatus { get; set; }
    public int SecondaryLimit { get; set; } = 100;
    public float SecondarySetCondition { get; set; }
    public float SecondaryKv { get; set; }
    public float SecondaryCurrent { get; set; }
    public int SecondaryTimeRequired { get; set; } = 60;
    public int SecondaryTestTime { get; set; }

    // 4LVB (Four Lead Vector Bushing) test data
    public TestStatus FourLvbStatus { get; set; } = new();
    public long SetCondition { get; set; }
    public float FourLvbSetCondition { get; set; }
    public int FourLvbTestTime { get; set; }
    public int FourLvbLimit { get; set; } = 100;
    public float FourLvbKv { get; set; }
    public float FourLvbCurrent { get; set; }
    public int FourLvbTimeRequired { get; set; } = 60;
}

/// <summary>
/// Collection of hipot test data for multiple test runs.
/// </summary>
[Serializable]
public class HipotTests : List<HipotData>
{
}
