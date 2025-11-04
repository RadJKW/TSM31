// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace TSM31.Core.Services;

using Microsoft.EntityFrameworkCore;
using Models;
using Persistence;
using Persistence.Entities;

/// <summary>
/// Telemetry and history storage service using SQLite database.
/// Migrated from browser localStorage for proper persistence across all host types.
/// Thread-safe singleton service for tracking downloads, test executions, and providing autocomplete data.
/// </summary>
public class TelemetryStorageService : ITelemetryStorageService
{
    private readonly TSM31StateDbContext _dbContext;
    private readonly SemaphoreSlim _lock = new(1, 1);

    // Legacy localStorage migration constants
    private const string LegacyStorageKey = "tsm31_telemetry_v1";

    public TelemetryStorageService(TSM31StateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordDownloadAsync(string serialNumber, string? catalogNumber, bool success,
        string? errorMessage = null, string? operatorId = null)
    {
        await _lock.WaitAsync();
        try
        {
            var downloadEvent = new TelemetryDownloadEvent
            {
                SerialNumber = serialNumber,
                CatalogNumber = catalogNumber,
                Success = success,
                ErrorMessage = errorMessage,
                OperatorId = operatorId,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.DownloadEvents.Add(downloadEvent);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to record download event: {ex.Message}");
            // Don't throw - telemetry failure shouldn't break app functionality
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RecordTestExecutionAsync(string serialNumber, string testType, int testNumber, string status,
        string? operatorId = null, string? notes = null)
    {
        await _lock.WaitAsync();
        try
        {
            var testEvent = new TelemetryTestEvent
            {
                SerialNumber = serialNumber,
                TestType = testType,
                TestNumber = testNumber,
                Status = status,
                OperatorId = operatorId,
                Notes = notes,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.TestEvents.Add(testEvent);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to record test execution event: {ex.Message}");
            // Don't throw - telemetry failure shouldn't break app functionality
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<SerialNumberSuggestion>> GetSerialNumberSuggestionsAsync(int maxResults = 10)
    {
        await _lock.WaitAsync();
        try
        {
            // Group by serial number and aggregate usage data
            var suggestions = await _dbContext.DownloadEvents
                .Where(e => e.Success) // Only successful downloads
                .GroupBy(e => e.SerialNumber)
                .Select(g => new SerialNumberSuggestion
                {
                    SerialNumber = g.Key,
                    LastUsed = g.Max(e => e.Timestamp),
                    UsageCount = g.Count(),
                    LastCatalogNumber = g.OrderByDescending(e => e.Timestamp).First().CatalogNumber
                })
                .OrderByDescending(s => s.LastUsed)
                .Take(maxResults)
                .ToListAsync();

            return suggestions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get serial number suggestions: {ex.Message}");
            return new List<SerialNumberSuggestion>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<UnitTelemetrySummary?> GetUnitSummaryAsync(string serialNumber)
    {
        await _lock.WaitAsync();
        try
        {
            var downloads = await _dbContext.DownloadEvents
                .Where(e => e.SerialNumber == serialNumber && e.Success)
                .ToListAsync();

            if (!downloads.Any())
                return null;

            var tests = await _dbContext.TestEvents
                .Where(e => e.SerialNumber == serialNumber)
                .ToListAsync();

            return new UnitTelemetrySummary
            {
                SerialNumber = serialNumber,
                FirstSeen = downloads.Min(d => d.Timestamp),
                LastSeen = downloads.Max(d => d.Timestamp),
                DownloadCount = downloads.Count,
                TotalTests = tests.Count,
                PassedTests = tests.Count(t => t.Status == "Passed"),
                FailedTests = tests.Count(t => t.Status == "Failed"),
                AbortedTests = tests.Count(t => t.Status == "Aborted")
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get unit summary: {ex.Message}");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<UnitDownloadEvent>> GetDownloadHistoryAsync(string serialNumber)
    {
        await _lock.WaitAsync();
        try
        {
            var events = await _dbContext.DownloadEvents
                .Where(e => e.SerialNumber == serialNumber)
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();

            return events.Select(e => new UnitDownloadEvent
            {
                SerialNumber = e.SerialNumber,
                CatalogNumber = e.CatalogNumber,
                Success = e.Success,
                ErrorMessage = e.ErrorMessage,
                OperatorId = e.OperatorId,
                Timestamp = e.Timestamp
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get download history: {ex.Message}");
            return new List<UnitDownloadEvent>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<TestExecutionEvent>> GetTestHistoryAsync(string serialNumber)
    {
        await _lock.WaitAsync();
        try
        {
            var events = await _dbContext.TestEvents
                .Where(e => e.SerialNumber == serialNumber)
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();

            return events.Select(e => new TestExecutionEvent
            {
                SerialNumber = e.SerialNumber,
                TestType = e.TestType,
                TestNumber = e.TestNumber,
                Status = e.Status,
                OperatorId = e.OperatorId,
                Notes = e.Notes,
                Timestamp = e.Timestamp
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get test history: {ex.Message}");
            return new List<TestExecutionEvent>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAllDataAsync()
    {
        await _lock.WaitAsync();
        try
        {
            // Remove all telemetry data (use with caution!)
            _dbContext.DownloadEvents.RemoveRange(_dbContext.DownloadEvents);
            _dbContext.TestEvents.RemoveRange(_dbContext.TestEvents);
            await _dbContext.SaveChangesAsync();

            Console.WriteLine("All telemetry data cleared from database");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear telemetry data: {ex.Message}");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> ExportDataAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var downloads = await _dbContext.DownloadEvents.ToListAsync();
            var tests = await _dbContext.TestEvents.ToListAsync();

            var export = new
            {
                ExportedAt = DateTime.UtcNow,
                DownloadEvents = downloads,
                TestEvents = tests
            };

            return System.Text.Json.JsonSerializer.Serialize(export,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to export telemetry data: {ex.Message}");
            return "{}";
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Optional cleanup method to remove old telemetry data.
    /// Call this periodically to prevent database growth.
    /// </summary>
    public async Task CleanupOldDataAsync(int daysToKeep = 90)
    {
        await _lock.WaitAsync();
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

            var oldDownloads = await _dbContext.DownloadEvents
                .Where(e => e.Timestamp < cutoffDate)
                .ToListAsync();

            var oldTests = await _dbContext.TestEvents
                .Where(e => e.Timestamp < cutoffDate)
                .ToListAsync();

            _dbContext.DownloadEvents.RemoveRange(oldDownloads);
            _dbContext.TestEvents.RemoveRange(oldTests);

            await _dbContext.SaveChangesAsync();

            Console.WriteLine($"Cleaned up telemetry: Removed {oldDownloads.Count} download events and {oldTests.Count} test events older than {daysToKeep} days");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to cleanup old telemetry data: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }
}
