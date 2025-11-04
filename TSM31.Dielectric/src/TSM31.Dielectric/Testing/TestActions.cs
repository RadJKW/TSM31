namespace TSM31.Dielectric.Testing;

/// <summary>
/// Generic test station navigation actions.
/// Test-station-specific implementations can extend this enum with additional actions.
/// </summary>
public enum TestActions
{
    /// <summary>
    /// Home/welcome screen
    /// </summary>
    Home,

    /// <summary>
    /// Review downloaded unit data
    /// </summary>
    DataReview,

    /// <summary>
    /// Enter or download unit data
    /// </summary>
    DataEntry,

    /// <summary>
    /// Generic testing screen
    /// </summary>
    Testing,

    /// <summary>
    /// Operator ID entry/login
    /// </summary>
    OperatorIdEntry,

    /// <summary>
    /// Settings or configuration screen
    /// </summary>
    Settings
}
