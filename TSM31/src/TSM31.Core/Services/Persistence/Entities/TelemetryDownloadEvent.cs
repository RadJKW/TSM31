namespace TSM31.Core.Services.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Records each unit download attempt for telemetry and autocomplete.
/// Migrated from browser localStorage to SQLite.
/// </summary>
[Table("TelemetryDownloadEvent")]
public class TelemetryDownloadEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string SerialNumber { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CatalogNumber { get; set; }

    /// <summary>
    /// Whether the download succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if download failed
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// ID of operator who performed the download
    /// </summary>
    [MaxLength(20)]
    public string? OperatorId { get; set; }

    /// <summary>
    /// UTC timestamp of the download event
    /// </summary>
    public DateTime Timestamp { get; set; }
}
