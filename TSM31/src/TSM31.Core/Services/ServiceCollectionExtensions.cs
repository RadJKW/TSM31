namespace TSM31.Core.Services;

using Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Persistence;
using TestData.Context;

/// <summary>
/// Extension methods for registering shared services across all host platforms
/// (Web, WinForms, MAUI, etc.)
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all shared services required by the TSM31.Core project.
    /// This includes FluentUI components, persistence services, and state management.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddFluentUIComponents(options => options.ValidateClassNames = false);

        services.AddLocalization();

        // Persistence Infrastructure (SQLite)
        // Use scoped to ensure proper DbContext lifecycle management
        services.AddScoped<TSM31StateDbContext>();
        services.AddScoped<AppStateStorageService>();

        services.AddScoped<InitializationStateService>();

        services.AddScoped<PubSubService>();
        services.AddScoped<EmployeeService>();

        // Console message service for displaying logs/messages to users
        services.AddSingleton<MessageConsoleService>();

        // Test data database (SQL Server)
        services.AddDbContext<TestDataDbContext>();

        services.AddScoped<UnitDataService>();
        services.AddScoped<TestConfigurationService>();

        // Options caching service (now uses SQLite instead of JSON files)
        services.AddScoped<OptionsStorageService>();

        // Telemetry storage (now uses SQLite instead of localStorage)
        services.AddScoped<ITelemetryStorageService, TelemetryStorageService>();

        // Use scoped lifetime so it can depend on scoped services like UnitDataService
        services.AddScoped<DielectricTestManager>();

        // Function key navigation service (depends on DielectricTestManager)
        services.AddScoped<FunctionKeyService>();

        // Startup orchestration services (for clean separation of layout initialization logic)
        services.AddScoped<SessionManager>();
        services.AddScoped<StartupService>();

        return services;
    }

    /// <summary>
    /// Initializes the SQLite database and ensures all tables are created.
    /// Call this once during application startup after services are configured.
    /// </summary>
    public static async Task InitializeStateDatabase(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TSM31StateDbContext>();
        await dbContext.EnsureDatabaseCreatedAsync();
    }
}
