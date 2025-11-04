using Microsoft.EntityFrameworkCore;
using TSM31.Dielectric.Operator;

namespace TSM31.Dielectric.DataManagement;

/// <summary>
/// Base SQLite database context for test station state persistence.
/// Provides session management, operator tracking, and telemetry storage.
/// Test-station-specific contexts can extend this to add custom tables.
/// </summary>
public class AppStateDbContext : DbContext
{
    private readonly string? _customDatabasePath;

    public DbSet<SessionState> SessionStates { get; set; } = null!;
    public DbSet<TelemetryEvent> TelemetryEvents { get; set; } = null!;

    /// <summary>
    /// Primary constructor - used by DI container.
    /// Database path is configured via DbContextOptions in ServiceCollectionExtensions.
    /// </summary>
    public AppStateDbContext(DbContextOptions<AppStateDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Constructor for testing or custom scenarios where a specific database path is needed.
    /// </summary>
    public AppStateDbContext(string customDatabasePath)
    {
        _customDatabasePath = customDatabasePath;

        // Ensure directory exists
        var directory = Path.GetDirectoryName(customDatabasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure if using custom path constructor (for testing/special scenarios)
        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_customDatabasePath))
        {
            optionsBuilder.UseSqlite($"Data Source={_customDatabasePath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // configure maxlengths for Employee properties if needed in derived classes
        modelBuilder.Entity<Employee>(entity => {
            entity.Property(e => e.Id).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.SuperVisorId).HasMaxLength(10);
        });
        // Configure SessionState
        modelBuilder.Entity<SessionState>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LastUpdated).IsRequired();
            entity.Property(e => e.CurrentAction).IsRequired();
            entity.Property(e => e.OperatorId).HasMaxLength(10);
            entity.Property(e => e.OperatorName).HasMaxLength(100);
            entity.Property(e => e.CurrentSerialNumber).HasMaxLength(50);
            entity.HasIndex(e => e.LastUpdated);
            // Ignore the Operator navigation property - we store OperatorId/OperatorName directly
            entity.Ignore(e => e.Operator);
        });

        // Configure TelemetryEvent
        modelBuilder.Entity<TelemetryEvent>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SerialNumber).HasMaxLength(50);
            entity.Property(e => e.OperatorId).HasMaxLength(50);
            entity.Property(e => e.Details).HasMaxLength(500);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.SerialNumber);
        });

        // Allow derived contexts to add their own tables
        ConfigureTestSpecificTables(modelBuilder);
    }

    /// <summary>
    /// Override this method in derived contexts to configure test-station-specific tables.
    /// </summary>
    protected virtual void ConfigureTestSpecificTables(ModelBuilder modelBuilder)
    {
        // Override in derived contexts
    }

    /// <summary>
    /// Ensures the database and all tables are created.
    /// Call this once during application startup.
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync()
    {
        await Database.EnsureCreatedAsync();
    }
}
