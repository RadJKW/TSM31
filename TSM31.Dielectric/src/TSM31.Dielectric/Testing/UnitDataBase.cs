namespace TSM31.Dielectric.Testing;

/// <summary>
/// Abstract base class for unit/device data in test stations.
/// Test-station-specific implementations should extend this with additional properties.
/// </summary>
public abstract class UnitDataBase
{
    /// <summary>
    /// Unique serial number identifying the unit
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Catalog/part number for the unit
    /// </summary>
    public string CatalogNumber { get; set; } = string.Empty;

    /// <summary>
    /// Work order number (if applicable)
    /// </summary>
    public string WorkOrder { get; set; } = string.Empty;

    /// <summary>
    /// Customer name
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Currently selected test index (1-based)
    /// </summary>
    public int CurrentTest { get; set; } = 1;

    /// <summary>
    /// Total number of tests required for this unit
    /// </summary>
    public int TotalTests { get; set; } = 1;

    /// <summary>
    /// Timestamp when unit data was downloaded/loaded
    /// </summary>
    public DateTime DownloadedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Indicates whether the unit has been downloaded/loaded
    /// </summary>
    public bool IsDownloaded { get; set; }

    // Additional properties for data bar display

    /// <summary>
    /// Unit type or classification (optional, for display purposes)
    /// </summary>
    public virtual string UnitType => "Standard";

    /// <summary>
    /// KVA rating (if applicable to the device type)
    /// </summary>
    public virtual float Kva { get; set; }

    /// <summary>
    /// Number of primary bushings (if applicable)
    /// </summary>
    public virtual int PrimaryBushings { get; set; } = 2;

    /// <summary>
    /// Number of primary ratings/taps (if applicable)
    /// </summary>
    public virtual int PrimaryRatings { get; set; } = 1;

    /// <summary>
    /// Number of secondary bushings (if applicable)
    /// </summary>
    public virtual int SecondaryBushings { get; set; } = 3;

    /// <summary>
    /// Number of secondary ratings/taps (if applicable)
    /// </summary>
    public virtual int SecondaryRatings { get; set; } = 1;

    /// <summary>
    /// Whether the unit has an arrestor (if applicable)
    /// </summary>
    public virtual bool HasArrestor { get; set; }

    /// <summary>
    /// Collection of voltage/current ratings for the unit.
    /// Override this property to provide test-station-specific ratings data.
    /// </summary>
    public virtual List<RatingData> Ratings { get; set; } = new();

    /// <summary>
    /// Validates that the unit data is complete and valid.
    /// Implement test-station-specific validation logic.
    /// </summary>
    public abstract bool IsValid();

    /// <summary>
    /// Returns a user-friendly display name for the unit
    /// </summary>
    public virtual string DisplayName =>
        !string.IsNullOrEmpty(SerialNumber) ? $"S/N: {SerialNumber}" : "No Unit Loaded";
}

/// <summary>
/// Represents voltage and current ratings for a unit.
/// </summary>
public class RatingData
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
