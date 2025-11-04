namespace TSM31.Core.Services.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents unit data (transformer) associated with a session.
/// This replaces the UnitDataJson column in AppSessionState with a structured, queryable format.
/// </summary>
[Table("SessionUnit")]
public class SessionUnit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to AppSessionState. One-to-one relationship.
    /// </summary>
    public int SessionStateId { get; set; }

    // Basic identification
    public string SerialNumber { get; set; } = "0000000000";
    public string WorkOrder { get; set; } = "00000";
    public string CatalogNumber { get; set; } = "0000000000000";
    public string CustomerName { get; set; } = "Valued Customer";
    public int CheckNumber { get; set; }

    // Unit characteristics
    public float Kva { get; set; }
    public int UnitType { get; set; } = 1; // Maps to TransformerType enum
    public bool IsDownloaded { get; set; }
    public bool IsManualEntry { get; set; }
    public bool IsSideBySide { get; set; }
    public string PolarityDesign { get; set; } = "A";

    // Test configuration
    public int CurrentTest { get; set; } = 1;
    public int TotalTests { get; set; } = 1;

    // Primary winding properties
    public int PrimaryBushings { get; set; } = 2;
    public string PrimaryCoilConfiguration { get; set; } = " ";
    public string PrimaryMaterial { get; set; } = "C";
    public int PrimaryRatings { get; set; } = 1;

    // Secondary winding properties
    public int SecondaryBushings { get; set; } = 3;
    public string SecondaryCoilConfiguration { get; set; } = " ";
    public string SecondaryMaterial { get; set; } = "A";
    public int SecondaryRatings { get; set; } = 1;

    // Optional components
    public bool HasArrestor { get; set; }
    public bool HasDisconnect { get; set; }

    // Regulator-specific properties (nullable for non-regulator units)
    public string? RegulatorType { get; set; }
    public int RegulatorVoltageRating { get; set; }
    public int RegulatorBil { get; set; }
    public int RegulatorHipotSetCondition { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SessionStateId))]
    public AppSessionState SessionState { get; set; } = null!;

    public ICollection<SessionRating> Ratings { get; set; } = new List<SessionRating>();
    public ICollection<SessionHipotTest> HipotTests { get; set; } = new List<SessionHipotTest>();
    public ICollection<SessionInducedTest> InducedTests { get; set; } = new List<SessionInducedTest>();
    public ICollection<SessionImpulseTest> ImpulseTests { get; set; } = new List<SessionImpulseTest>();
}
