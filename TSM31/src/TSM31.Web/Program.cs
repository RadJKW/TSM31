using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Tailwind;
using TSM31.Core.Services;
using TSM31.Core.Services.Config;
using TSM31.Core.Services.Contracts;
using TSM31.Web.Components;
using TSM31.Web.Services;
using _Imports = TSM31.Core._Imports;

// ═══════════════════════════════════════════════════════════
// PHASE 1: Bootstrap Serilog for Early Logging
// ═══════════════════════════════════════════════════════════

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code, applyThemeToRedirectedOutput:true)
    .CreateBootstrapLogger();

Log.Information("TSM31 Web host starting...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ═══════════════════════════════════════════════════════════
    // PHASE 2: Configure Serilog from appsettings.json
    // ═══════════════════════════════════════════════════════════

    builder.Host.UseSerilog((context, services, configuration) => {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId();

        // Get MessageConsoleService and add sink
        var messageConsoleService = services.GetRequiredService<MessageConsoleService>();
        var uiMinLevel = context.Configuration.GetValue<string>("MessageConsoleUI:MinimumLevel") ?? "Information";
        var uiLogLevel = Enum.Parse<LogEventLevel>(uiMinLevel);

        configuration.WriteTo.MessageConsoleSink(messageConsoleService, restrictedToMinimumLevel: uiLogLevel);
    });

    Log.Information("Serilog configured from appsettings.json");

    // Log environment details
    var isDevelopment = builder.Environment.IsDevelopment();
    var diagnosticPath = isDevelopment
        ? @"C:\TestStation_TSM31\diagnostic"
        : @"C:\TestStation\diagnostic";

    Directory.CreateDirectory(diagnosticPath);
    Log.Information("Running in {Environment} mode, logs at {DiagnosticPath}",
        builder.Environment.EnvironmentName, diagnosticPath);

    // ═══════════════════════════════════════════════════════════
    // PHASE 3: Add Services
    // ═══════════════════════════════════════════════════════════

    Log.Debug("Configuring services...");

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Add shared services (FluentUI, DbContext, KeyboardNavigation, Localization, etc.)
    builder.Services.AddCoreServices();

    // Add platform-specific services for the Web project
    builder.Services.AddSingleton<IFormFactor, FormFactor>();
    builder.Services.AddSingleton<IExceptionHandler, WebExceptionHandler>();

    Log.Information("Services configured");

    // ═══════════════════════════════════════════════════════════
    // PHASE 4: Build and Configure Pipeline
    // ═══════════════════════════════════════════════════════════

    var app = builder.Build();

    Log.Information("Application built");

    // Initialize SQLite state database
    Log.Information("Initializing SQLite state database...");
    await ServiceCollectionExtensions.InitializeStateDatabase(app.Services);
    Log.Information("SQLite state database initialized");

    // Configure the HTTP request pipeline.
    Log.Debug("Configuring HTTP request pipeline...");

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
        Log.Information("Production middleware configured (exception handler, HSTS)");
    }
    else
    {
        _ = app.RunTailwind("tailwind", "../TSM31.Core/");
        Log.Information("Development middleware configured (Tailwind)");
    }

    app.UseAntiforgery();
    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddAdditionalAssemblies(typeof(_Imports).Assembly);

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
    Log.Information("TSM31 Web host shutting down");
    Log.CloseAndFlush();
}
