namespace TSM31.Dielectric.DataManagement;

/// <summary>
/// Represents a telemetry event for tracking test station operations
/// </summary>
public class TelemetryEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string EventType { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? OperatorId { get; set; }
    public string? Details { get; set; }
}
