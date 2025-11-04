namespace TSM31.TestData.Models;

/// <summary>
/// Contains calibration coefficients for high voltage and low voltage hipot measurements.
/// </summary>
public class HipotCoefficients
{
    public HipotCoefficient HvTare { get; set; } = new();
    public HipotCoefficient LvTare { get; set; } = new();
}

/// <summary>
/// Polynomial coefficients for hipot meter calibration (C0 + C1*x + C2*x^2).
/// </summary>
public class HipotCoefficient
{
    public float Bias { get; set; }
    public float C0 { get; set; }
    public float C1 { get; set; } = 1;
    public float C2 { get; set; }
}
