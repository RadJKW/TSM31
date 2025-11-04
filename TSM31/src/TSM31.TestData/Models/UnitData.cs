namespace TSM31.TestData.Models;

public class UnitData
{
    // Internal identifiers
    internal string MachineId { get; set; } = "0000";
    internal string OperatorId { get; set; } = "0000";
    internal string SupervisorId { get; set; } = "0000";

    // Basic properties with defaults
    public bool HasArrestor { get; set; }
    public string CatalogNumber { get; set; } = "0000000000000";
    public int CheckNumber { get; set; }
    public int CurrentTest { get; set; } = 1;
    public bool HasDisconnect { get; set; }
    public bool IsDownloaded { get; set; }
    public float Kva { get; set; }
    public int PrimaryBushings { get; set; } = 2;
    public string PrimaryCoilConfiguration { get; set; } = " ";
    public string PrimaryMaterial { get; set; } = "C";
    public int PrimaryRatings { get; set; } = 1;
    public int SecondaryBushings { get; set; } = 3;
    public string SecondaryCoilConfiguration { get; set; } = " ";
    public string SecondaryMaterial { get; set; } = "A";
    public int SecondaryRatings { get; set; } = 1;
    public int TotalTests { get; set; } = 1;
    public TransformerType UnitType { get; set; } = TransformerType.SinglePhase;
    public bool IsManualEntry { get; set; }
    public string CustomerName { get; set; } = "Valued Customer";
    public string WorkOrder { get; set; } = "00000";
    public bool IsSideBySide { get; set; }
    public string PolarityDesign { get; set; } = "A";

    // Regulator-specific properties
    public string? RegulatorType { get; set; }
    public int RegulatorVoltageRating { get; set; }
    public int RegulatorBil { get; set; }
    public int RegulatorHipotSetCondition { get; set; }

    // Serial number with special setter logic
    private string _serialNumber = "0000000000";
    public string SerialNumber
    {
        get => _serialNumber;
        set
        {
            // Remove hyphen if present (format: 000000-0000 -> 0000000000)
            if (value.Contains('-'))
            {
                _serialNumber = value.Substring(0, 6) + value.Substring(7, 4);
            }
            else
            {
                _serialNumber = value;
            }
        }
    }

    // Test data collections
    public HipotTests Hipot { get; set; } = new();
    public ImpulseTests Impulse { get; set; } = new();
    public InducedTests Induced { get; set; } = new();
    public RatingsClass Ratings { get; set; } = new();

    // Note: BuildPacket method intentionally omitted - this appears to be legacy serialization
    // that should be refactored into a separate service/formatter class in modern architecture
}

public enum TransformerType
{
    SinglePhase = 1,
    ThreePhaseDy = 5,
    ThreePhaseYd = 6,
    ThreePhaseDd = 7,
    ThreePhaseYy = 8,
    Regulator = 9,
    StepDown = 10
}
