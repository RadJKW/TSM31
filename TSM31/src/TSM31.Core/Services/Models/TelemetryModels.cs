// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace TSM31.Core.Services.Models;

/// <summary>
/// Represents a unit download event for telemetry tracking.
/// </summary>
public class UnitDownloadEvent
{
    public string SerialNumber { get; set; } = string.Empty;
    public string? CatalogNumber { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OperatorId { get; set; }
}

/// <summary>
/// Represents a test execution event (completion, failure, abort).
/// </summary>
public class TestExecutionEvent
{
    public string SerialNumber { get; set; } = string.Empty;
    public string TestType { get; set; } = string.Empty; // "Hipot", "Induced", "Impulse"
    public int TestNumber { get; set; } // 1-based test index
    public string Status { get; set; } = string.Empty; // "Passed", "Failed", "Aborted"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? OperatorId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Aggregated telemetry summary for a serial number.
/// </summary>
public class UnitTelemetrySummary
{
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public int DownloadCount { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int AbortedTests { get; set; }
}

/// <summary>
/// Serial number suggestion with usage metadata for autocomplete.
/// </summary>
public class SerialNumberSuggestion
{
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime LastUsed { get; set; }
    public int UsageCount { get; set; }
    public string? LastCatalogNumber { get; set; }

    /// <summary>
    /// Display text for autocomplete: "12345 (last used: 2 hours ago)"
    /// </summary>
    public string DisplayText => $"{SerialNumber} (last used: {GetRelativeTime()})";

    private string GetRelativeTime()
    {
        var delta = DateTime.UtcNow - LastUsed;
        if (delta.TotalMinutes < 1) return "just now";
        if (delta.TotalHours < 1) return $"{(int)delta.TotalMinutes} min ago";
        if (delta.TotalDays < 1) return $"{(int)delta.TotalHours} hours ago";
        if (delta.TotalDays < 7) return $"{(int)delta.TotalDays} days ago";
        return LastUsed.ToLocalTime().ToString("MM/dd/yyyy");
    }
}

/// <summary>
/// Complete telemetry storage model (persisted to localStorage).
/// </summary>
public class TelemetryStorage
{
    public List<UnitDownloadEvent> DownloadHistory { get; set; } = new();
    public List<TestExecutionEvent> TestHistory { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Maximum number of download events to retain (prevent unbounded growth).
    /// </summary>
    public const int MaxDownloadHistory = 1000;

    /// <summary>
    /// Maximum number of test events to retain.
    /// </summary>
    public const int MaxTestHistory = 5000;
}
