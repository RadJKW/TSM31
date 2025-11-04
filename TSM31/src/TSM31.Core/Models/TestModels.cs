namespace TSM31.Core.Models;

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

public class TestData
{
    public int TestNumber { get; set; } = 1;
    public decimal SetConditionKv { get; set; }
    public string VoltageRange { get; set; } = string.Empty;
    public string CurrentRange { get; set; } = string.Empty;
    public decimal WattLimit { get; set; }
}

public class HipotTestData
{
    public int TestNumber { get; set; }
    public decimal PrimarySetVoltage { get; set; }
    public decimal PrimaryCurrentLimit { get; set; }
    public decimal SecondarySetVoltage { get; set; }
    public decimal SecondaryCurrentLimit { get; set; }
    public decimal FourLvbSetVoltage { get; set; }
    public decimal FourLvbCurrentLimit { get; set; }
    public int PrimaryTime { get; set; }
    public int SecondaryTime { get; set; }
    public int FourLvbTime { get; set; }
    public string Status { get; set; } = "Not Required";
}

public class ImpulseTestData
{
    public int TestNumber { get; set; }
    public decimal H1Measured { get; set; }
    public string H1Status { get; set; } = "Required";
    public decimal H2Measured { get; set; }
    public string H2Status { get; set; } = "Not Required";
    public decimal H3Measured { get; set; }
    public string H3Status { get; set; } = "Not Required";
    public decimal X1Measured { get; set; }
    public string X1Status { get; set; } = "Not Required";
    public decimal X2Measured { get; set; }
    public string X2Status { get; set; } = "Not Required";
}

public class InducedTestData
{
    public int TestNumber { get; set; }
    public decimal FirstVoltsAvg { get; set; }
    public decimal FirstAmpsAvg { get; set; }
    public decimal FirstWattsAvg { get; set; }
    public int FirstTime { get; set; }
    public string FirstStatus { get; set; } = "Not Required";
    public decimal SecondVoltsAvg { get; set; }
    public decimal SecondAmpsAvg { get; set; }
    public decimal SecondWattsAvg { get; set; }
    public int SecondTime { get; set; }
    public string SecondStatus { get; set; } = "Required";
}

public class MeterReading
{
    public decimal Ch1Vrms { get; set; }
    public decimal Ch1Irms { get; set; }
    public decimal Ch1Power { get; set; }
    public decimal Ch2Vrms { get; set; }
    public decimal Ch2Irms { get; set; }
    public decimal Ch2Power { get; set; }
    public decimal Ch3Vrms { get; set; }
    public decimal Ch3Irms { get; set; }
    public decimal Ch3Power { get; set; }
}
