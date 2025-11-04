// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace TSM31.Dielectric.Database.Entities;

/// <summary>
/// Test parameters entity for transformer dielectric testing.
/// Maps to the Params table in the TestData SQL Server database.
/// Contains test specifications, limits, and requirements for each test number.
/// </summary>
public class Param
{
    public string TestNumber { get; set; } = null!;
    public string WorkOrder { get; set; } = null!;
    public string CatalogNumber { get; set; } = null!;
    public string Kva { get; set; } = null!;
    public string Pv { get; set; } = null!;  // Primary Voltage
    public string Sv { get; set; } = null!;  // Secondary Voltage
    public string UnitType { get; set; } = null!;

    // Winding configuration
    public string PriBushings { get; set; } = null!;
    public string SecBushings { get; set; } = null!;
    public string PriRatings { get; set; } = null!;
    public string SecRatings { get; set; } = null!;
    public string PriCoilCfg { get; set; } = null!;
    public string SecCoilCfg { get; set; } = null!;
    public string PriMaterial { get; set; } = null!;
    public string SecMaterial { get; set; } = null!;
    public string Polarity { get; set; } = null!;
    public string RefTemp { get; set; } = null!;

    // BIL (Basic Impulse Level) ratings
    public string PriBil { get; set; } = null!;
    public string SecBil { get; set; } = null!;

    // Physical features
    public string Arrestor { get; set; } = null!;  // "Y" or blank
    public string DisconnectPresent { get; set; } = null!;  // "Y" or blank
    public string SideBySideFlag { get; set; } = null!;

    // Induced test parameters
    public string InducedVolts { get; set; } = null!;
    public string InducedWattsLimit { get; set; } = null!;
    public string FirstInducedRequired { get; set; } = null!;  // "R" or blank
    public string SecondInducedRequired { get; set; } = null!;  // "R" or blank
    public string SecondInducedTestTime { get; set; } = null!;

    // Hipot test parameters
    public string Lvhipotlimit { get; set; } = null!;  // Low voltage hipot limit (mA)
    public string Hvhipotlimit { get; set; } = null!;  // High voltage hipot limit (mA)
    public string PriHipotrequired { get; set; } = null!;  // "R" or blank
    public string SecHipotrequired { get; set; } = null!;  // "R" or blank
    public string FourLvbhipotRequired { get; set; } = null!;  // "R" or blank
    public string FourLvbhipotTestVoltage { get; set; } = null!;
    public string FourLvbhipotTestTime { get; set; } = null!;

    // Impulse test requirements
    public string H1ImpulseRequired { get; set; } = null!;  // "R" or blank
    public string H2ImpulseRequired { get; set; } = null!;
    public string H3ImpulseRequired { get; set; } = null!;
    public string X1ImpulseRequired { get; set; } = null!;
    public string X2ImpulseRequired { get; set; } = null!;
    public string X3ImpulseRequired { get; set; } = null!;

    // Loss test parameters
    public string StrayLoss { get; set; } = null!;
    public string CoreMaxQuoted { get; set; } = null!;
    public string CoreMaxDesign { get; set; } = null!;
    public string CoreMinDesign { get; set; } = null!;
    public string PercentIexMaxQuoted { get; set; } = null!;
    public string PercentIexMaxDesign { get; set; } = null!;
    public string CondMaxQuoted { get; set; } = null!;
    public string CondMaxDesign { get; set; } = null!;
    public string CondMinDesign { get; set; } = null!;
    public string PercentIzMaxQuoted { get; set; } = null!;
    public string PercentIzMaxDesign { get; set; } = null!;
    public string PercentIzMinQuoted { get; set; } = null!;
    public string PercentIzMinDesign { get; set; } = null!;

    // Test counts
    public string TotalDielectricTests { get; set; } = null!;
    public string TotalLossTests { get; set; } = null!;

    // Additional test requirements
    public string SecTestVolts { get; set; } = null!;
    public string CoreLossRequired { get; set; } = null!;
    public string CondLossAndIzRequired { get; set; } = null!;
    public string RatioAndBalanceRequired { get; set; } = null!;
    public string PercentIexRequired { get; set; } = null!;

    // DOE (Department of Energy) parameters
    public string DoewattsLimit { get; set; } = null!;
    public string DoecustomerLimit { get; set; } = null!;
    public string AuxiliaryDeviceWattsAt85 { get; set; } = null!;
    public string AuxiliaryDeviceMaterialFactor { get; set; } = null!;
    public string DoeconductorLossMultiplier { get; set; } = null!;
}
