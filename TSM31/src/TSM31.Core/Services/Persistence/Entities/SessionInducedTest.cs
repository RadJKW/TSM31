namespace TSM31.Core.Services.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents induced voltage test data for first and second tests.
/// </summary>
[Table("SessionInducedTest")]
public class SessionInducedTest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to SessionUnit
    /// </summary>
    public int SessionUnitId { get; set; }

    /// <summary>
    /// 1-based test number (corresponds to array index + 1 in UnitData.Induced collection)
    /// </summary>
    public int TestNumber { get; set; }

    // First induced test
    public int FirstStatus { get; set; } // TestStatusType enum value
    public string FirstLimitText { get; set; } = string.Empty;
    public string? FirstVoltStatus { get; set; }
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
    public int SecondStatus { get; set; } // TestStatusType enum value
    public string SecondLimitText { get; set; } = string.Empty;
    public string? SecondVoltStatus { get; set; }
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

    // Navigation property
    [ForeignKey(nameof(SessionUnitId))]
    public SessionUnit SessionUnit { get; set; } = null!;
}
