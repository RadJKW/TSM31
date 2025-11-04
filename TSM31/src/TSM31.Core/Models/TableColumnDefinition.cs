namespace TSM31.Core.Models;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Defines a column for the generic DataTable component.
/// </summary>
/// <typeparam name="TItem">The type of data being displayed in the table</typeparam>
public class TableColumnDefinition<TItem>
{
    /// <summary>
    /// The header text to display for this column
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Function to extract the value from the item for this column
    /// </summary>
    public Func<TItem, object> ValueSelector { get; set; } = _ => string.Empty;

    /// <summary>
    /// Optional format string for numeric values (e.g., "0.000", "0.00")
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Optional CSS class to apply to cells in this column
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// Optional custom render function for complex cell content (e.g., badges)
    /// </summary>
    public RenderFragment<TItem>? CustomRender { get; set; }
}
