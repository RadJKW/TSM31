namespace TSM31.Core.Services.Persistence.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Cached BIL options from database to reduce repeated queries.
/// Migrated from JSON file storage to SQLite.
/// </summary>
[Table("OptionsCache")]
public class OptionsCacheEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// JSON array of primary BIL values (e.g., ["095", "110", "125", ...])
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string PrimaryBilsJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of secondary BIL values
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string SecondaryBilsJson { get; set; } = "[]";

    /// <summary>
    /// Timestamp of when this cache was last updated from the database
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Flag indicating this is the current/active cache entry.
    /// Only one entry should have IsCurrent = true.
    /// </summary>
    public bool IsCurrent { get; set; }
}
