namespace TSM31.Core.Services.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Records each test execution event for telemetry and audit trail.
/// Migrated from browser localStorage to SQLite.
/// </summary>
[Table("TelemetryTestEvent")]
public class TelemetryTestEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Type of test (Hipot, Induced, Impulse)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TestType { get; set; } = string.Empty;

    /// <summary>
    /// Test number (1-based index for multi-test units)
    /// </summary>
    public int TestNumber { get; set; }

    /// <summary>
    /// Test status (Passed, Failed, Aborted)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// ID of operator who performed the test
    /// </summary>
    [MaxLength(20)]
    public string? OperatorId { get; set; }

    /// <summary>
    /// Optional notes about the test execution
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// UTC timestamp of the test event
    /// </summary>
    public DateTime Timestamp { get; set; }
}
