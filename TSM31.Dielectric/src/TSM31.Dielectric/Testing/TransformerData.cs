namespace TSM31.Dielectric.Testing;

/// <summary>
/// View model for transformer data entry UI.
/// Simplified representation for display and editing in DataEntryTab.
/// </summary>
public class TransformerData
{
    public string SerialNumber { get; set; } = "000000-0000";
    public string CatalogNumber { get; set; } = "0000-000000-000";
    public int CheckNumber { get; set; } = 0;
    public string UnitType { get; set; } = "Side By Side";
    public decimal Kva { get; set; } = 75.0m;
    public string WorkOrder { get; set; } = "00000";
    public string Customer { get; set; } = "Valued Customer";

    // Transformer Specifications
    public int PrimaryVoltage { get; set; } = 14400;
    public int PrimaryRatings { get; set; } = 1;
    public int PrimaryBushings { get; set; } = 1;
    public string PrimaryBIL { get; set; } = "000";

    public int SecondaryVoltage { get; set; } = 120;
    public int SecondaryRatings { get; set; } = 1;
    public int SecondaryBushings { get; set; } = 1;
    public string SecondaryBIL { get; set; } = "000";

    public bool Arrestor { get; set; } = true;

    // Test Requirements (Impulse Tests)
    public string H1Impulse { get; set; } = "Not Required";
    public string H2Impulse { get; set; } = "Not Required";
    public string H3Impulse { get; set; } = "Not Required";
    public string X1Impulse { get; set; } = "Not Required";
    public string X2Impulse { get; set; } = "Not Required";

    // Test Requirements (Hipot Tests)
    public string PrimaryHipot { get; set; } = "Not Required";
    public string SecondaryHipot { get; set; } = "Not Required";
    public string FourLvbHipot { get; set; } = "Not Required";

    // Test Requirements (Induced Tests)
    public string FirstInduced { get; set; } = "Not Required";
    public string SecondInduced { get; set; } = "Not Required";
}
