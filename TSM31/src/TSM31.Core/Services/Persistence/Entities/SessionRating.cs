namespace TSM31.Core.Services.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents voltage and current ratings for a transformer unit.
/// Multiple ratings can exist for units with different tap configurations.
/// </summary>
[Table("SessionRating")]
public class SessionRating
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to SessionUnit
    /// </summary>
    public int SessionUnitId { get; set; }

    /// <summary>
    /// 1-based test number (corresponds to array index + 1 in UnitData.Ratings collection)
    /// </summary>
    public int TestNumber { get; set; }

    // Primary ratings
    public float PrimaryVoltage { get; set; }
    public float PrimaryCurrent { get; set; }
    public int PrimaryBIL { get; set; }

    // Secondary ratings
    public float SecondaryVoltage { get; set; }
    public float SecondaryCurrent { get; set; }
    public int SecondaryBIL { get; set; }

    /// <summary>
    /// Design ratio (Primary Voltage / Secondary Voltage).
    /// Stored for queryability even though it's calculable.
    /// </summary>
    public float DesignRatio { get; set; }

    // Navigation property
    [ForeignKey(nameof(SessionUnitId))]
    public SessionUnit SessionUnit { get; set; } = null!;
}
