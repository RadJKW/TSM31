namespace TSM31.TestData.Models;

/// <summary>
/// Represents raw and corrected meter readings from a hipot test.
/// </summary>
public class HipotMeterReading
{
    // High voltage hipot measurements
    public float HighVoltageKilovolts { get; set; }
    public float HighVoltageMilliamps { get; set; }
    public float HighVoltageKilovoltsRaw { get; set; }
    public float HighVoltageMilliampsRaw { get; set; }

    // Low voltage hipot measurements
    public float LowVoltageKilovolts { get; set; }
    public float LowVoltageMilliamps { get; set; }
    public float LowVoltageKilovoltsRaw { get; set; }
    public float LowVoltageMilliampsRaw { get; set; }
}
