// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace TSM31.Core.Services;

using Models;

/// <summary>
/// Interface for telemetry and history storage service.
/// Tracks unit downloads, test executions, and provides autocomplete suggestions.
/// </summary>
public interface ITelemetryStorageService
{
    /// <summary>
    /// Records a unit download event.
    /// </summary>
    Task RecordDownloadAsync(string serialNumber, string? catalogNumber, bool success, string? errorMessage = null, string? operatorId = null);

    /// <summary>
    /// Records a test execution event (passed, failed, or aborted).
    /// </summary>
    Task RecordTestExecutionAsync(string serialNumber, string testType, int testNumber, string status, string? operatorId = null, string? notes = null);

    /// <summary>
    /// Gets serial number suggestions for autocomplete, ordered by most recent usage.
    /// </summary>
    /// <param name="maxResults">Maximum number of suggestions to return (default: 10)</param>
    Task<List<SerialNumberSuggestion>> GetSerialNumberSuggestionsAsync(int maxResults = 10);

    /// <summary>
    /// Gets telemetry summary for a specific serial number.
    /// </summary>
    Task<UnitTelemetrySummary?> GetUnitSummaryAsync(string serialNumber);

    /// <summary>
    /// Gets all download events for a serial number.
    /// </summary>
    Task<List<UnitDownloadEvent>> GetDownloadHistoryAsync(string serialNumber);

    /// <summary>
    /// Gets all test execution events for a serial number.
    /// </summary>
    Task<List<TestExecutionEvent>> GetTestHistoryAsync(string serialNumber);

    /// <summary>
    /// Clears all telemetry data (for maintenance/reset purposes).
    /// </summary>
    Task ClearAllDataAsync();

    /// <summary>
    /// Exports telemetry data as JSON for backup/analysis.
    /// </summary>
    Task<string> ExportDataAsync();
}
