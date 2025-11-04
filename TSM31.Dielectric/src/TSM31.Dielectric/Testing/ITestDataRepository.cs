namespace TSM31.Dielectric.Testing;

/// <summary>
/// Generic interface for downloading/loading unit data from a data source.
/// Implement this interface in your test-station-specific repository.
/// </summary>
/// <typeparam name="TUnitData">Your test-station-specific unit data type</typeparam>
public interface ITestDataRepository<TUnitData> where TUnitData : class
{
    /// <summary>
    /// Downloads unit data by identifier (serial number, catalog number, etc.)
    /// </summary>
    /// <param name="identifier">Unique identifier for the unit</param>
    /// <returns>Unit data if found, null otherwise</returns>
    Task<TUnitData?> DownloadUnitAsync(string identifier);

    /// <summary>
    /// Validates that a unit identifier exists in the data source
    /// </summary>
    /// <param name="identifier">Unique identifier to validate</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ValidateUnitExistsAsync(string identifier);
}
