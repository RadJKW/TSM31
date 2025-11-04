namespace TSM31.TestData.Models;

/// <summary>
/// Contains calibration coefficients for potential transformers (PT) and current transformers (CT).
/// </summary>
public class InducedCoefficients
{
    // Potential Transformers
    public PotentialTransformer Pt1 { get; set; } = new();
    public PotentialTransformer Pt2 { get; set; } = new();
    public PotentialTransformer Pt3 { get; set; } = new();
    public PotentialTransformer Pt4 { get; set; } = new();
    public PotentialTransformer Pt5 { get; set; } = new();

    // Current Transformers with different ranges
    public CurrentTransformer Ct1050 { get; set; } = new();
    public CurrentTransformer Ct1175 { get; set; } = new();
    public CurrentTransformer Ct2050 { get; set; } = new();
    public CurrentTransformer Ct2175 { get; set; } = new();
    public CurrentTransformer Ct3050 { get; set; } = new();
    public CurrentTransformer Ct3175 { get; set; } = new();
    public CurrentTransformer Ct4050 { get; set; } = new();
    public CurrentTransformer Ct4150 { get; set; } = new();
    public CurrentTransformer Ct5005 { get; set; } = new();
    public CurrentTransformer Ct6003 { get; set; } = new();
}

/// <summary>
/// Calibration coefficient for a potential transformer.
/// </summary>
public class PotentialTransformer
{
    public float Coefficient { get; set; }
}

/// <summary>
/// Calibration coefficient for a current transformer.
/// </summary>
public class CurrentTransformer
{
    public float Coefficient { get; set; }
}
