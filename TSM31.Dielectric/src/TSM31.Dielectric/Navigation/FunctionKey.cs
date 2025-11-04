namespace TSM31.Dielectric.Navigation;

/// <summary>
/// Represents a function key with its label, handler, and enabled state.
/// </summary>
public record FunctionKey
{
    /// <summary>Key identifier (e.g., "F1", "ESC")</summary>
    public string Key { get; init; }

    /// <summary>Display label for the key (shown in menu)</summary>
    public string Label { get; set; }

    /// <summary>Action to execute when key is pressed</summary>
    public Action Action { get; set; }

    /// <summary>Whether the key is currently enabled</summary>
    public bool IsEnabled { get; set; }

    public FunctionKey(string key, string label, Action action, bool isEnabled = true)
    {
        Key = key;
        Label = label;
        Action = action;
        IsEnabled = isEnabled;
    }
}
