namespace TSM31.Dielectric.Database.Entities;

/// <summary>
/// Cross-reference table entity for mapping serial numbers to catalog numbers and work orders.
/// Maps to the Xref table in the TestData SQL Server database.
/// </summary>
public class Xref
{
    /// <summary>
    /// Serial number (10 characters)
    /// </summary>
    public string Serno { get; set; } = null!;

    /// <summary>
    /// Catalog number (13 characters max)
    /// </summary>
    public string Catno { get; set; } = null!;

    /// <summary>
    /// Work order (5 characters)
    /// </summary>
    public string Workorder { get; set; } = null!;
}
