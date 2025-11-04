namespace TSM31.Core.Services.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents impulse test data for H1, H2, H3, X1, X2, and X3 bushings.
/// </summary>
[Table("SessionImpulseTest")]
public class SessionImpulseTest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to SessionUnit
    /// </summary>
    public int SessionUnitId { get; set; }

    /// <summary>
    /// 1-based test number (corresponds to array index + 1 in UnitData.Impulse collection)
    /// </summary>
    public int TestNumber { get; set; }

    // H1 bushing impulse test
    public int H1ShotCounter { get; set; }
    public int H1Status { get; set; } // TestStatusType enum value
    public int H1WaveformCompareStatus { get; set; } // TestStatusType enum value
    public float H1Voltage { get; set; }

    // H2 bushing impulse test
    public int H2ShotCounter { get; set; }
    public int H2Status { get; set; } // TestStatusType enum value
    public int H2WaveformCompareStatus { get; set; } // TestStatusType enum value
    public float H2Voltage { get; set; }

    // H3 bushing impulse test
    public int H3ShotCounter { get; set; }
    public int H3Status { get; set; } // TestStatusType enum value
    public int H3WaveformCompareStatus { get; set; } // TestStatusType enum value
    public float H3Voltage { get; set; }

    // X1 bushing impulse test
    public int X1ShotCounter { get; set; }
    public int X1Status { get; set; } // TestStatusType enum value
    public int X1WaveformCompareStatus { get; set; } // TestStatusType enum value
    public float X1Voltage { get; set; }

    // X2 bushing impulse test
    public int X2ShotCounter { get; set; }
    public int X2Status { get; set; } // TestStatusType enum value
    public int X2WaveformCompareStatus { get; set; } // TestStatusType enum value
    public float X2Voltage { get; set; }

    // X3 bushing impulse test
    public int X3ShotCounter { get; set; }
    public int X3Status { get; set; } // TestStatusType enum value
    public int X3WaveformCompareStatus { get; set; } // TestStatusType enum value
    public float X3Voltage { get; set; }

    // Test configuration
    public float SetCondition { get; set; }
    public float SecondarySetCondition { get; set; }

    // Navigation property
    [ForeignKey(nameof(SessionUnitId))]
    public SessionUnit SessionUnit { get; set; } = null!;
}
