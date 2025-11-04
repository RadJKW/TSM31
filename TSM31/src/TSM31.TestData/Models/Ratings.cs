namespace TSM31.TestData.Models;

/// <summary>
/// Represents voltage and current ratings for a transformer.
/// </summary>
[Serializable]
public class Ratings
{
    public float PrimaryVoltage { get; set; }
    public float PrimaryCurrent { get; set; }
    public int PrimaryBIL { get; set; }

    public float SecondaryVoltage { get; set; }
    public float SecondaryCurrent { get; set; }
    public int SecondaryBIL { get; set; }

    /// <summary>
    /// Calculated design ratio (Primary Voltage / Secondary Voltage).
    /// </summary>
    public float DesignRatio => SecondaryVoltage != 0 ? PrimaryVoltage / SecondaryVoltage : 0;
}

/// <summary>
/// Collection of ratings for transformers with multiple tap configurations.
/// </summary>
[Serializable]
public class RatingsClass : List<Ratings>
{
}
