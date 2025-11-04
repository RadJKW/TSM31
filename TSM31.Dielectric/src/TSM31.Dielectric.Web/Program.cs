using TSM31.Dielectric.Console;
using TSM31.Dielectric.Common;
using TSM31.Dielectric.Configuration;
using TSM31.Dielectric.Web;
using Serilog;
using Serilog.Events;
using Tailwind;
using TSM31.Dielectric.Testing;

// ═══════════════════════════════════════════════════════════
// PHASE 1: Bootstrap Serilog for Early Logging
// ═══════════════════════════════════════════════════════════

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(
        outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("TSM31.Dielectric Web host starting...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ═══════════════════════════════════════════════════════════
    // PHASE 2: Add Services (BEFORE Serilog configuration)
    // ═══════════════════════════════════════════════════════════

    Log.Debug("Configuring services...");

    // Add services to the container
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Add Core test station services
    // Add all test station services (features + infrastructure)
    builder.Services.AddTestStationServices();

    // Add test station manager implementation
    builder.Services.AddScoped<ITestManager, TestManager>();

    Log.Information("Services configured");

    // ═══════════════════════════════════════════════════════════
    // PHASE 3: Configure Serilog with MessageConsoleSink
    // ═══════════════════════════════════════════════════════════

    // Read PathOptions and update Serilog file path before configuring Serilog
    var pathOptions = builder.Configuration.GetSection("ApplicationPaths").Get<PathOptions>();
    if (pathOptions != null)
    {
        var logPath = Path.Combine(pathOptions.LogsDirectory, "log_.txt");

        // Ensure logs directory exists
        Directory.CreateDirectory(pathOptions.LogsDirectory);

        // Update the configuration value before Serilog reads it
        builder.Configuration["Serilog:WriteTo:0:Args:path"] = logPath;

        Log.Debug("Serilog file path configured: {LogPath}", logPath);
    }

    builder.Logging.ClearProviders();
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        // Get MessageConsoleService from DI (now available since services are registered)
        var messageConsoleService = services.GetRequiredService<MessageConsoleService>();
        var uiMinLevel = context.Configuration.GetValue<string>("MessageConsoleUI:MinimumLevel") ?? "Information";
        var uiLogLevel = Enum.Parse<LogEventLevel>(uiMinLevel);

        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.MessageConsoleSink(messageConsoleService, restrictedToMinimumLevel: uiLogLevel);

        Log.Information("Serilog configured with MessageConsoleSink (minimum level: {MinLevel})", uiLogLevel);
    }, preserveStaticLogger: true, writeToProviders: true);

    Log.Information("Serilog configured from appsettings.json");

    // ═══════════════════════════════════════════════════════════
    // PHASE 4: Build Application
    // ═══════════════════════════════════════════════════════════

    var app = builder.Build();

    Log.Information("Application built");

    // ═══════════════════════════════════════════════════════════
    // PHASE 5: Initialize State Database
    // ═══════════════════════════════════════════════════════════

    Log.Information("Initializing SQLite state database...");
    await app.Services.InitializeStateDatabaseAsync();
    Log.Information("SQLite state database initialized");

    // ═══════════════════════════════════════════════════════════
    // PHASE 6: Configure HTTP Request Pipeline
    // ═══════════════════════════════════════════════════════════

    Log.Debug("Configuring HTTP request pipeline...");

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
        Log.Information("Production middleware configured (exception handler, HSTS)");
    }
    else
    {
        _ = app.RunTailwind("tailwind", "../TSM31.Dielectric/");
        Log.Information("Development middleware configured (Tailwind)");
    }

    // Add antiforgery middleware as required by InteractiveServerRenderMode
    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddAdditionalAssemblies(typeof(TSM31.Dielectric.UI.Routes).Assembly);

    Log.Information("Application configured and ready to start");
    Log.Information("Starting web server on {Urls}", string.Join(", ", app.Urls));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("TSM31.Dielectric Web host shutting down");
    Log.CloseAndFlush();
}
