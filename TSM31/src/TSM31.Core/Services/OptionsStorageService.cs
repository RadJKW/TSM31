namespace TSM31.Core.Services;

using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;
using Persistence.Entities;

/// <summary>
/// Manages caching of database-derived options (like BIL values) to SQLite.
/// Migrated from JSON file storage for better reliability and query performance.
/// Reduces database calls by loading options once at startup and persisting them locally.
/// </summary>
public class OptionsStorageService
{
    private readonly UnitDataService _unitDataService;
    private readonly TSM31StateDbContext _dbContext;
    private readonly ILogger<OptionsStorageService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<string> _primaryBilsCache = [];
    private List<string> _secondaryBilsCache = [];

    private const string ProductionPath = @"C:\TestStation";
    private const string DevelopmentPath = @"C:\TestStation_TSM31";
    private const string LegacyCacheFileName = "options_cache.json";

    public IEnumerable<string> PrimaryBILs => _primaryBilsCache;
    public IEnumerable<string> SecondaryBILs => _secondaryBilsCache;
    private bool IsLoaded { get; set; }

    public OptionsStorageService(
        UnitDataService unitDataService,
        TSM31StateDbContext dbContext,
        ILogger<OptionsStorageService> logger)
    {
        _unitDataService = unitDataService;
        _dbContext = dbContext;
        _logger = logger;

        // Determine legacy JSON path for migration
        var basePath = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
            ? DevelopmentPath
            : ProductionPath;
        Path.Combine(basePath, LegacyCacheFileName);
    }

    /// <summary>
    /// Initializes the options cache by loading from SQLite or migrating from legacy JSON.
    /// Call this at application startup.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _logger.LogTrace("InitializeAsync started, lock acquired");

            // Try to load from SQLite first
            if (await LoadFromSqliteAsync())
            {
                _logger.LogDebug("Loaded options from SQLite database");
                IsLoaded = true;
                return;
            }

            // No cache available, load from database
            _logger.LogDebug("No cached options found, loading from database...");

            // Call internal refresh method without re-acquiring lock (we already hold it)
            await RefreshFromDatabaseInternalAsync();
        }
        finally
        {
            _logger.LogTrace("InitializeAsync releasing lock");
            _lock.Release();
        }
    }

    /// <summary>
    /// Refreshes options from the database and updates the SQLite cache.
    /// Call this when you want to force a database refresh.
    /// </summary>
    public async Task RefreshFromDatabaseAsync()
    {
        _logger.LogDebug("RefreshFromDatabaseAsync (public) called, acquiring lock...");
        await _lock.WaitAsync();
        try
        {
            await RefreshFromDatabaseInternalAsync();
        }
        finally
        {
            _logger.LogTrace("RefreshFromDatabaseAsync releasing lock");
            _lock.Release();
        }
    }

    /// <summary>
    /// Internal method that performs the actual database refresh without acquiring the lock.
    /// Called by InitializeAsync (which already holds the lock) and RefreshFromDatabaseAsync.
    /// </summary>
    private async Task RefreshFromDatabaseInternalAsync()
    {
        _logger.LogDebug("RefreshFromDatabaseInternalAsync started");
        try
        {
            _logger.LogDebug("Requesting distinct primary BILs...");
            var primaryBilsResult = await _unitDataService.GetDistinctPrimaryBILs();
            var primaryBils = primaryBilsResult.ToList();
            _logger.LogDebug("Retrieved {PrimaryBilCount} primary BIL values", primaryBils.Count);

            _logger.LogDebug("Requesting distinct secondary BILs...");
            var secondaryBilsResult = await _unitDataService.GetDistinctSecondaryBILs();
            var secondaryBils = secondaryBilsResult.ToList();
            _logger.LogDebug("Retrieved {SecondaryBilCount} secondary BIL values", secondaryBils.Count);

            _primaryBilsCache = primaryBils;
            _secondaryBilsCache = secondaryBils;

            IsLoaded = true;

            // Save to SQLite
            _logger.LogDebug("Persisting refreshed options to SQLite...");
            await SaveToSqliteAsync();
            _logger.LogDebug("Persisted refreshed options to SQLite");

            _logger.LogInformation("Successfully refreshed options from database and saved to SQLite");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh options from database");

            // If we don't have any cached data and DB fails, we're in trouble
            if (!IsLoaded)
            {
                throw new InvalidOperationException(
                "Failed to load options from both cache and database. Application cannot continue.", ex);
            }
        }
    }

    private async Task<bool> LoadFromSqliteAsync()
    {
        try
        {
            var currentCache = await _dbContext.OptionsCache
                .Where(c => c.IsCurrent)
                .OrderByDescending(c => c.LastUpdated)
                .FirstOrDefaultAsync();

            if (currentCache == null)
                return false;

            var primaryBils = JsonSerializer.Deserialize<List<string>>(currentCache.PrimaryBilsJson);
            var secondaryBils = JsonSerializer.Deserialize<List<string>>(currentCache.SecondaryBilsJson);

            if (primaryBils == null || secondaryBils == null || !primaryBils.Any() || !secondaryBils.Any())
            {
                _logger.LogWarning("SQLite cache exists but contains invalid data");
                return false;
            }

            _primaryBilsCache = primaryBils;
            _secondaryBilsCache = secondaryBils;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading from SQLite");
            return false;
        }
    }

    private async Task SaveToSqliteAsync()
    {
        try
        {
            _logger.LogTrace("SaveToSqliteAsync invoked");
            // Mark all existing caches as non-current
            var existingCaches = await _dbContext.OptionsCache
                .Where(c => c.IsCurrent)
                .ToListAsync();

            _logger.LogTrace("Found {ExistingCacheCount} existing cache records to update", existingCaches.Count);

            foreach (var cache in existingCaches)
            {
                cache.IsCurrent = false;
            }

            _logger.LogTrace("Existing cache records marked as non-current");

            // Create new cache entry
            var newCache = new OptionsCacheEntity {
                PrimaryBilsJson = JsonSerializer.Serialize(_primaryBilsCache),
                SecondaryBilsJson = JsonSerializer.Serialize(_secondaryBilsCache),
                LastUpdated = DateTime.UtcNow,
                IsCurrent = true
            };

            _logger.LogTrace("New cache prepared: PrimaryCount={PrimaryCount}, SecondaryCount={SecondaryCount}",
                _primaryBilsCache.Count, _secondaryBilsCache.Count);

            _dbContext.OptionsCache.Add(newCache);
            _logger.LogTrace("New cache entity added to DbContext");
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("SaveChangesAsync completed successfully");
        }
        catch (Exception ex)
        {
            // Don't throw - saving cache is not critical if we already have data in memory
            _logger.LogError(ex, "Failed to save options to SQLite");
        }
    }
}
