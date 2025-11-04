namespace TSM31.Core.Services.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents a transformer unit tested at the station.
/// This is a permanent historical record - one row per unique (SerialNumber + WorkOrder).
/// Accumulates test history and is never deleted.
/// </summary>
[Table("Unit")]
public class Unit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Basic identification
    [MaxLength(20)]
    public string SerialNumber { get; set; } = "0000000000";

    [MaxLength(20)]
    public string WorkOrder { get; set; } = "00000";

    [MaxLength(50)]
    public string CatalogNumber { get; set; } = "0000000000000";

    [MaxLength(200)]
    public string CustomerName { get; set; } = "Valued Customer";
    public int CheckNumber { get; set; }

    // Unit characteristics
    public float Kva { get; set; }
    public int UnitType { get; set; } = 1; // Maps to TransformerType enum
    public bool IsDownloaded { get; set; }
    public bool IsManualEntry { get; set; }
    public bool IsSideBySide { get; set; }

    [MaxLength(10)]
    public string PolarityDesign { get; set; } = "A";

    // Test configuration
    public int CurrentTest { get; set; } = 1;
    public int TotalTests { get; set; } = 1;

    // Primary winding properties
    public int PrimaryBushings { get; set; } = 2;

    [MaxLength(10)]
    public string PrimaryCoilConfiguration { get; set; } = " ";

    [MaxLength(10)]
    public string PrimaryMaterial { get; set; } = "C";

    public int PrimaryRatings { get; set; } = 1;

    // Secondary winding properties
    public int SecondaryBushings { get; set; } = 3;

    [MaxLength(10)]
    public string SecondaryCoilConfiguration { get; set; } = " ";

    [MaxLength(10)]
    public string SecondaryMaterial { get; set; } = "A";
    public int SecondaryRatings { get; set; } = 1;

    // Optional components
    public bool HasArrestor { get; set; }
    public bool HasDisconnect { get; set; }

    // Regulator-specific properties (nullable for non-regulator units)
    [MaxLength(50)]
    public string? RegulatorType { get; set; }

    public int RegulatorVoltageRating { get; set; }
    public int RegulatorBil { get; set; }
    public int RegulatorHipotSetCondition { get; set; }

    // Operator tracking - who downloaded this unit
    [MaxLength(20)]
    public string? OperatorId { get; set; }

    [MaxLength(100)]
    public string? OperatorName { get; set; }

    [MaxLength(20)]
    public string? SupervisorId { get; set; }

    // Timestamps
    /// <summary>
    /// When this unit was downloaded from the Params table
    /// </summary>
    public DateTime DownloadedAt { get; set; }

    /// <summary>
    /// Last time any field on this unit or its tests was modified
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<HipotTest> HipotTests { get; set; } = new List<HipotTest>();
    public ICollection<InducedTest> InducedTests { get; set; } = new List<InducedTest>();
    public ICollection<ImpulseTest> ImpulseTests { get; set; } = new List<ImpulseTest>();
}
