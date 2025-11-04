namespace TSM31.Core.Services.Persistence;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// SQLite database context for persisting application state, options cache, and telemetry.
/// Enables recovery from crashes, power loss, and provides audit trail for all operations.
/// </summary>
public class TSM31StateDbContext : DbContext
{
    private readonly string _databasePath;
    private readonly ILogger<TSM31StateDbContext>? _logger;

    public DbSet<AppSessionState> SessionStates { get; set; } = null!;
    public DbSet<Unit> Units { get; set; } = null!;
    public DbSet<Rating> Ratings { get; set; } = null!;
    public DbSet<HipotTest> HipotTests { get; set; } = null!;
    public DbSet<InducedTest> InducedTests { get; set; } = null!;
    public DbSet<ImpulseTest> ImpulseTests { get; set; } = null!;
    public DbSet<OptionsCacheEntity> OptionsCache { get; set; } = null!;
    public DbSet<TelemetryDownloadEvent> DownloadEvents { get; set; } = null!;
    public DbSet<TelemetryTestEvent> TestEvents { get; set; } = null!;

    public TSM31StateDbContext() : this(null)
    {
    }

    public TSM31StateDbContext(ILogger<TSM31StateDbContext>? logger)
    {
        _logger = logger;

        // Determine database path based on environment
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        var basePath = isDevelopment ? @"C:\TestStation_TSM31" : @"C:\TestStation";
        var databaseDir = Path.Combine(basePath, "Database");

        // Ensure directory exists
        if (!Directory.Exists(databaseDir))
        {
            try
            {
                Directory.CreateDirectory(databaseDir);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create database directory");
                // Fallback to temp directory if production path fails
                databaseDir = Path.Combine(Path.GetTempPath(), "TSM31", "Database");
                Directory.CreateDirectory(databaseDir);
            }
        }

        _databasePath = Path.Combine(databaseDir, "tsm31.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_databasePath}");

            // Enable detailed errors in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AppSessionState indexes
        modelBuilder.Entity<AppSessionState>()
            .HasIndex(s => s.IsCurrent)
            .HasDatabaseName("IX_AppSessionState_IsCurrent");

        modelBuilder.Entity<AppSessionState>()
            .HasIndex(s => s.SerialNumber)
            .HasDatabaseName("IX_AppSessionState_SerialNumber");

        modelBuilder.Entity<AppSessionState>()
            .HasIndex(s => s.OperatorId)
            .HasDatabaseName("IX_AppSessionState_OperatorId");

        modelBuilder.Entity<AppSessionState>()
            .HasIndex(s => s.CreatedAt)
            .HasDatabaseName("IX_AppSessionState_CreatedAt");

        // OptionsCache indexes
        modelBuilder.Entity<OptionsCacheEntity>()
            .HasIndex(o => o.IsCurrent)
            .HasDatabaseName("IX_OptionsCache_IsCurrent");

        // TelemetryDownloadEvent indexes
        modelBuilder.Entity<TelemetryDownloadEvent>()
            .HasIndex(e => e.SerialNumber)
            .HasDatabaseName("IX_TelemetryDownloadEvent_SerialNumber");

        modelBuilder.Entity<TelemetryDownloadEvent>()
            .HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_TelemetryDownloadEvent_Timestamp");

        modelBuilder.Entity<TelemetryDownloadEvent>()
            .HasIndex(e => e.Success)
            .HasDatabaseName("IX_TelemetryDownloadEvent_Success");

        modelBuilder.Entity<TelemetryDownloadEvent>()
            .HasIndex(e => e.OperatorId)
            .HasDatabaseName("IX_TelemetryDownloadEvent_OperatorId");

        // TelemetryTestEvent indexes
        modelBuilder.Entity<TelemetryTestEvent>()
            .HasIndex(e => e.SerialNumber)
            .HasDatabaseName("IX_TelemetryTestEvent_SerialNumber");

        modelBuilder.Entity<TelemetryTestEvent>()
            .HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_TelemetryTestEvent_Timestamp");

        modelBuilder.Entity<TelemetryTestEvent>()
            .HasIndex(e => e.TestType)
            .HasDatabaseName("IX_TelemetryTestEvent_TestType");

        modelBuilder.Entity<TelemetryTestEvent>()
            .HasIndex(e => e.Status)
            .HasDatabaseName("IX_TelemetryTestEvent_Status");

        modelBuilder.Entity<TelemetryTestEvent>()
            .HasIndex(e => e.OperatorId)
            .HasDatabaseName("IX_TelemetryTestEvent_OperatorId");

        // AppSessionState to Unit relationship (many sessions can point to same unit)
        modelBuilder.Entity<AppSessionState>()
            .HasOne(s => s.Unit)
            .WithMany()
            .HasForeignKey(s => s.UnitId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unit configuration
        modelBuilder.Entity<Unit>()
            .HasIndex(u => new { u.SerialNumber, u.WorkOrder })
            .IsUnique()
            .HasDatabaseName("IX_Unit_SerialNumber_WorkOrder");

        modelBuilder.Entity<Unit>()
            .HasIndex(u => u.SerialNumber)
            .HasDatabaseName("IX_Unit_SerialNumber");

        modelBuilder.Entity<Unit>()
            .HasIndex(u => u.OperatorId)
            .HasDatabaseName("IX_Unit_OperatorId");

        modelBuilder.Entity<Unit>()
            .HasIndex(u => u.DownloadedAt)
            .HasDatabaseName("IX_Unit_DownloadedAt");

        // Rating configuration
        modelBuilder.Entity<Rating>()
            .HasOne(r => r.Unit)
            .WithMany(u => u.Ratings)
            .HasForeignKey(r => r.UnitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Rating>()
            .HasIndex(r => new { r.UnitId, r.TestNumber })
            .HasDatabaseName("IX_Rating_UnitId_TestNumber");

        // HipotTest configuration
        modelBuilder.Entity<HipotTest>()
            .HasOne(h => h.Unit)
            .WithMany(u => u.HipotTests)
            .HasForeignKey(h => h.UnitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HipotTest>()
            .HasIndex(h => new { h.UnitId, h.TestNumber })
            .HasDatabaseName("IX_HipotTest_UnitId_TestNumber");

        modelBuilder.Entity<HipotTest>()
            .HasIndex(h => h.PrimaryStatus)
            .HasDatabaseName("IX_HipotTest_PrimaryStatus");

        modelBuilder.Entity<HipotTest>()
            .HasIndex(h => h.SecondaryStatus)
            .HasDatabaseName("IX_HipotTest_SecondaryStatus");

        modelBuilder.Entity<HipotTest>()
            .HasIndex(h => h.CompletedAt)
            .HasDatabaseName("IX_HipotTest_CompletedAt");

        // InducedTest configuration
        modelBuilder.Entity<InducedTest>()
            .HasOne(i => i.Unit)
            .WithMany(u => u.InducedTests)
            .HasForeignKey(i => i.UnitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InducedTest>()
            .HasIndex(i => new { i.UnitId, i.TestNumber })
            .HasDatabaseName("IX_InducedTest_UnitId_TestNumber");

        modelBuilder.Entity<InducedTest>()
            .HasIndex(i => i.FirstStatus)
            .HasDatabaseName("IX_InducedTest_FirstStatus");

        modelBuilder.Entity<InducedTest>()
            .HasIndex(i => i.SecondStatus)
            .HasDatabaseName("IX_InducedTest_SecondStatus");

        modelBuilder.Entity<InducedTest>()
            .HasIndex(i => i.FirstCompletedAt)
            .HasDatabaseName("IX_InducedTest_FirstCompletedAt");

        modelBuilder.Entity<InducedTest>()
            .HasIndex(i => i.SecondCompletedAt)
            .HasDatabaseName("IX_InducedTest_SecondCompletedAt");

        // ImpulseTest configuration
        modelBuilder.Entity<ImpulseTest>()
            .HasOne(i => i.Unit)
            .WithMany(u => u.ImpulseTests)
            .HasForeignKey(i => i.UnitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ImpulseTest>()
            .HasIndex(i => new { i.UnitId, i.TestNumber })
            .HasDatabaseName("IX_ImpulseTest_UnitId_TestNumber");

        modelBuilder.Entity<ImpulseTest>()
            .HasIndex(i => i.H1Status)
            .HasDatabaseName("IX_ImpulseTest_H1Status");

        modelBuilder.Entity<ImpulseTest>()
            .HasIndex(i => i.X1Status)
            .HasDatabaseName("IX_ImpulseTest_X1Status");

        modelBuilder.Entity<ImpulseTest>()
            .HasIndex(i => i.CompletedAt)
            .HasDatabaseName("IX_ImpulseTest_CompletedAt");
    }

    /// <summary>
    /// Ensures the database and tables are created.
    /// Call this once at application startup.
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync()
    {
        try
        {
            await Database.EnsureCreatedAsync();
            _logger?.LogInformation("SQLite database initialized at: {DatabasePath}", _databasePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize SQLite database");
            throw;
        }
    }

    /// <summary>
    /// Gets the full path to the database file
    /// </summary>
    public string GetDatabasePath() => _databasePath;
}
