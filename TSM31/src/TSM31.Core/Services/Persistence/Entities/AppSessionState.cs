namespace TSM31.Core.Services.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents a saved application session state including operator and unit data.
/// Enables recovery from crashes, power loss, and intentional app restarts.
/// </summary>
[Table("AppSessionState")]
public class AppSessionState
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Operator information
    [MaxLength(20)]
    public string? OperatorId { get; set; }

    [MaxLength(100)]
    public string? OperatorName { get; set; }

    [MaxLength(20)]
    public string? SupervisorId { get; set; }

    // Unit data information
    [MaxLength(20)]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Foreign key to the current unit being tested (links to historical Unit table).
    /// Null when no unit is loaded in the session.
    /// </summary>
    public int? UnitId { get; set; }

    /// <summary>
    /// Navigation property to the associated unit in the historical database.
    /// </summary>
    [ForeignKey(nameof(UnitId))]
    public Unit? Unit { get; set; }

    /// <summary>
    /// Current test action/screen (e.g., "DataReview", "HipotTest", etc.)
    /// </summary>
    [MaxLength(50)]
    public string? CurrentTestAction { get; set; }

    /// <summary>
    /// TestStationData serialized as JSON (test station configuration)
    /// </summary>
    [MaxLength(4000)]
    public string? TestStationDataJson { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Flag indicating this is the active/current session.
    /// Only one session should have IsCurrent = true at a time.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Optional notes about how the session ended (manual logout, crash, etc.)
    /// </summary>
    [MaxLength(200)]
    public string? SessionEndReason { get; set; }
}
