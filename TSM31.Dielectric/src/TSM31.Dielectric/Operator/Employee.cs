namespace TSM31.Dielectric.Operator;

/// <summary>
/// Represents an operator/employee who can log into the test station.
/// </summary>
public class Employee
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Automatically generated initials from employee name.
    /// Supports 2 or 3 part names (e.g., "John Doe" → "DJ", "John Michael Doe" → "DMJ")
    /// </summary>
    public string Initials
    {
        get {
            var nameParts = Name.Split(' ');
            return nameParts.Length switch {
                3 => $"{nameParts[1][0]}{nameParts[2][0]}{nameParts[0][0]}",
                2 => $"{nameParts[1][0]}{nameParts[0][0]}",
                _ => string.Empty
            };
        }
    }

    public string SuperVisorId { get; set; } = string.Empty;
    public bool IsValid { get; init; }
}
