using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using TSM31.Dielectric.Console;
using TSM31.Dielectric.Navigation;
using TSM31.Dielectric.Operator;
using TSM31.Dielectric.Configuration;
using TSM31.Dielectric.DataManagement;
using TSM31.Dielectric.Database;
using TSM31.Dielectric.Testing;

namespace TSM31.Dielectric.Common;

/// <summary>
/// Extension methods for registering all test station services.
/// Test-station-specific implementations should call AddTestStationServices() first,
/// then register their own services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all test station services (features + infrastructure).
    /// Call this first, then register test-station-specific services.
    /// </summary>
    public static void AddTestStationServices(this IServiceCollection services)
    {
        // Configuration Options
        // Build a temporary service provider to access IConfiguration
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        // Register PathOptions from configuration
        services.Configure<PathOptions>(configuration.GetSection("ApplicationPaths"));

        // UI Components
        services.AddFluentUIComponents();

        // Feature Services
        services.AddScoped<EmployeeService>();
        services.AddScoped<FunctionKeyService>();
        services.AddSingleton<MessageConsoleService>();
        services.AddScoped<InitializationStateService>();
        services.AddScoped<StartupService>();

        // Infrastructure Services (Database)
        services.AddDbContextFactory<AppStateDbContext>((sp, options) =>
        {
            var pathOptions = sp.GetRequiredService<IOptions<PathOptions>>().Value;
            var databaseDir = pathOptions.DatabaseDirectory;

            // Ensure directory exists
            Directory.CreateDirectory(databaseDir);

            var databasePath = Path.Combine(databaseDir, "teststation.db");
            options.UseSqlite($"Data Source={databasePath}");
        });

        services.AddScoped<SessionManager>();
        services.AddScoped<IAppStateStorageService, AppStateStorageService>();

        // Dielectric-specific services (SQL Server test data database)
        services.AddDbContext<TestDataDbContext>(options =>
        {
            // Connection string can be overridden via appsettings.json
            var connectionString = configuration.GetConnectionString("TestData")
                ?? "Server=RAD-SQL;Database=TestData;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;";
            options.UseSqlServer(connectionString);
        });

        // Register test data repository
        services.AddScoped<ITestDataRepository<UnitData>, TestDataRepository>();

        // Register test manager (concrete implementation)
        services.AddScoped<TestManager>();
        services.AddScoped<ITestManager>(sp => sp.GetRequiredService<TestManager>());
    }

    /// <summary>
    /// Ensures the SQLite state database is created.
    /// Call this once during application startup (after services are built).
    /// </summary>
    public static async Task InitializeStateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppStateDbContext>();
        await dbContext.EnsureDatabaseCreatedAsync();
    }
}
