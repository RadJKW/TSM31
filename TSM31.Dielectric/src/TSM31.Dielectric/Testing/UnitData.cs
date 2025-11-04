namespace TSM31.Dielectric.Testing;

/// <summary>
/// Concrete unit data implementation for dielectric transformer testing.
/// Extends the template's UnitDataBase with dielectric-specific properties.
/// </summary>
[Serializable]
public class UnitData : UnitDataBase
{
    // Internal identifiers
    internal string MachineId { get; set; } = "0000";
    internal string OperatorId { get; set; } = "0000";
    internal string SupervisorId { get; set; } = "0000";

    // Override base properties with specific defaults
    public override float Kva { get; set; }
    public override int PrimaryBushings { get; set; } = 2;
    public override int PrimaryRatings { get; set; } = 1;
    public override int SecondaryBushings { get; set; } = 3;
    public override int SecondaryRatings { get; set; } = 1;
    public override bool HasArrestor { get; set; }

    // Dielectric-specific properties
    public int CheckNumber { get; set; }
    public bool HasDisconnect { get; set; }
    public string PrimaryCoilConfiguration { get; set; } = " ";
    public string PrimaryMaterial { get; set; } = "C";  // C = Copper, A = Aluminum
    public string SecondaryCoilConfiguration { get; set; } = " ";
    public string SecondaryMaterial { get; set; } = "A";
    public bool IsManualEntry { get; set; }
    public bool IsSideBySide { get; set; }
    public string PolarityDesign { get; set; } = "A";
    public TransformerType TransformerUnitType { get; set; } = TransformerType.SinglePhase;

    // Regulator-specific properties
    public string? RegulatorType { get; set; }
    public int RegulatorVoltageRating { get; set; }
    public int RegulatorBil { get; set; }
    public int RegulatorHipotSetCondition { get; set; }

    // Override UnitType to return string representation of TransformerUnitType
    public override string UnitType => TransformerUnitType switch
    {
        TransformerType.SinglePhase => "Single Phase",
        TransformerType.ThreePhaseDy => "Three Phase Dy",
        TransformerType.ThreePhaseYd => "Three Phase Yd",
        TransformerType.ThreePhaseDd => "Three Phase Dd",
        TransformerType.ThreePhaseYy => "Three Phase Yy",
        TransformerType.Regulator => "Regulator",
        TransformerType.StepDown => "Step Down",
        _ => "Unknown"
    };

    // Test data collections - using template's DielectricRatings instead of Ratings
    public HipotTests Hipot { get; set; } = new();
    public ImpulseTests Impulse { get; set; } = new();
    public InducedTests Induced { get; set; } = new();
    public DielectricRatingsClass DielectricRatings { get; set; } = new();

    // Override the base Ratings property to map to DielectricRatings
    public override List<RatingData> Ratings
    {
        get => DielectricRatings.Select(r => new RatingData
        {
            PrimaryVoltage = r.PrimaryVoltage,
            PrimaryCurrent = r.PrimaryCurrent,
            PrimaryBIL = r.PrimaryBIL,
            SecondaryVoltage = r.SecondaryVoltage,
            SecondaryCurrent = r.SecondaryCurrent,
            SecondaryBIL = r.SecondaryBIL
        }).ToList();
        set
        {
            DielectricRatings = new DielectricRatingsClass();
            foreach (var rating in value)
            {
                DielectricRatings.Add(new DielectricRatings
                {
                    PrimaryVoltage = rating.PrimaryVoltage,
                    PrimaryCurrent = rating.PrimaryCurrent,
                    PrimaryBIL = rating.PrimaryBIL,
                    SecondaryVoltage = rating.SecondaryVoltage,
                    SecondaryCurrent = rating.SecondaryCurrent,
                    SecondaryBIL = rating.SecondaryBIL
                });
            }
        }
    }

    /// <summary>
    /// Validates that the unit data is complete and valid.
    /// </summary>
    public override bool IsValid()
    {
        // Basic validation: must have serial number and catalog number
        if (string.IsNullOrWhiteSpace(SerialNumber) || SerialNumber == "0000000000")
            return false;

        if (string.IsNullOrWhiteSpace(CatalogNumber) || CatalogNumber == "0000000000000")
            return false;

        // Must have at least one test
        if (TotalTests < 1)
            return false;

        // Must have ratings data
        if (DielectricRatings == null || DielectricRatings.Count == 0)
            return false;

        return true;
    }
}

/// <summary>
/// Transformer unit type classification.
/// </summary>
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
