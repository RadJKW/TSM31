namespace TSM31.Core.Services.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents hipot test data for primary, secondary, and 4LVB tests.
/// Tracks when the test was started and completed for historical reporting.
/// </summary>
[Table("HipotTest")]
public class HipotTest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Unit
    /// </summary>
    public int UnitId { get; set; }

    /// <summary>
    /// 1-based test number (corresponds to array index + 1 in UnitData.Hipot collection)
    /// </summary>
    public int TestNumber { get; set; }

    // Primary hipot test data
    public int PrimaryStatus { get; set; }// TestStatusType enum value

    [MaxLength(50)]
    public string PrimaryLimitText { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PrimaryVoltStatus { get; set; }

    [MaxLength(50)]
    public string? PrimaryAmpStatus { get; set; }

    [MaxLength(50)]
    public string? PrimaryTimeStatus { get; set; }

    public int PrimaryLimit { get; set; } = 100;
    public float PrimarySetCondition { get; set; }
    public float PrimaryKv { get; set; }
    public float PrimaryCurrent { get; set; }
    public int PrimaryTimeRequired { get; set; } = 60;
    public int PrimaryTestTime { get; set; }

    // Secondary hipot test data
    public int SecondaryStatus { get; set; }// TestStatusType enum value

    [MaxLength(50)]
    public string SecondaryLimitText { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SecondaryVoltStatus { get; set; }

    [MaxLength(50)]
    public string? SecondaryAmpStatus { get; set; }

    [MaxLength(50)]
    public string? SecondaryTimeStatus { get; set; }

    public int SecondaryLimit { get; set; } = 100;
    public float SecondarySetCondition { get; set; }
    public float SecondaryKv { get; set; }
    public float SecondaryCurrent { get; set; }
    public int SecondaryTimeRequired { get; set; } = 60;
    public int SecondaryTestTime { get; set; }

    // 4LVB (Four Lead Vector Bushing) test data
    public int FourLvbStatus { get; set; }// TestStatusType enum value
    public long SetCondition { get; set; }
    public float FourLvbSetCondition { get; set; }
    public int FourLvbTestTime { get; set; }
    public int FourLvbLimit { get; set; } = 100;
    public float FourLvbKv { get; set; }
    public float FourLvbCurrent { get; set; }
    public int FourLvbTimeRequired { get; set; } = 60;

    // Timestamps for historical tracking
    /// <summary>
    /// When test power was turned on (test began)
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When test finished (passed, failed, or aborted)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Last time any field was updated (readings, status, etc.)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(UnitId))]
    public Unit Unit { get; set; } = null!;
}
