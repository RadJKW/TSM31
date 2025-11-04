namespace TSM31.Core.Services.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents induced voltage test data for first and second tests.
/// Tracks separate start/completion times for each test phase.
/// </summary>
[Table("InducedTest")]
public class InducedTest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Unit
    /// </summary>
    public int UnitId { get; set; }

    /// <summary>
    /// 1-based test number (corresponds to array index + 1 in UnitData.Induced collection)
    /// </summary>
    public int TestNumber { get; set; }

    // First induced test
    public int FirstStatus { get; set; }// TestStatusType enum value

    [MaxLength(50)]
    public string FirstLimitText { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? FirstVoltStatus { get; set; }

    [MaxLength(50)]
    public string? FirstWattStatus { get; set; }

    public int FirstTimeRequired { get; set; } = 4;
    public float FirstVoltage { get; set; }
    public float FirstPower { get; set; }
    public float FirstCurrent { get; set; }
    public int FirstTestTime { get; set; }

    // First test - three phase measurements
    public float FirstC1V { get; set; }
    public float FirstC1A { get; set; }
    public float FirstC1W { get; set; }
    public float FirstC2V { get; set; }
    public float FirstC2A { get; set; }
    public float FirstC2W { get; set; }
    public float FirstC3V { get; set; }
    public float FirstC3A { get; set; }
    public float FirstC3W { get; set; }

    // Second induced test
    public int SecondStatus { get; set; }// TestStatusType enum value

    [MaxLength(50)]
    public string SecondLimitText { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SecondVoltStatus { get; set; }

    [MaxLength(50)]
    public string? SecondWattStatus { get; set; }

    public int SecondTimeRequired { get; set; } = 18;
    public float SecondVoltage { get; set; }
    public float SecondPower { get; set; }
    public float SecondCurrent { get; set; }
    public int SecondTestTime { get; set; }

    // Second test - three phase measurements
    public float SecondC1V { get; set; }
    public float SecondC1A { get; set; }
    public float SecondC1W { get; set; }
    public float SecondC2V { get; set; }
    public float SecondC2A { get; set; }
    public float SecondC2W { get; set; }
    public float SecondC3V { get; set; }
    public float SecondC3A { get; set; }
    public float SecondC3W { get; set; }

    // Test configuration
    public int WattLimit { get; set; }
    public int SetCondition { get; set; }
    public int VoltageRange { get; set; }
    public long CurrentRange { get; set; }

    // Timestamps for historical tracking - separate for each test phase
    /// <summary>
    /// When first induced test power was turned on
    /// </summary>
    public DateTime? FirstStartedAt { get; set; }

    /// <summary>
    /// When first induced test finished
    /// </summary>
    public DateTime? FirstCompletedAt { get; set; }

    /// <summary>
    /// When second induced test power was turned on
    /// </summary>
    public DateTime? SecondStartedAt { get; set; }

    /// <summary>
    /// When second induced test finished
    /// </summary>
    public DateTime? SecondCompletedAt { get; set; }

    /// <summary>
    /// Last time any field was updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(UnitId))]
    public Unit Unit { get; set; } = null!;
}
