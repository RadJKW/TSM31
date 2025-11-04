using TSM31.Dielectric.Common;
using TSM31.Dielectric.UI;

namespace TSM31.Dielectric.WinForm;

using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Console;
using Configuration;
using Serilog;
using Serilog.Events;

public static partial class Program
{
    [STAThread]
    public static void Main()
    {
        // ═══════════════════════════════════════════════════════════
        // PHASE 0: Detect Debug Mode and Set Environment
        // ═══════════════════════════════════════════════════════════


        // ═══════════════════════════════════════════════════════════
        // PHASE 1: Bootstrap Serilog FIRST (before anything else)
        // ═══════════════════════════════════════════════════════════

        // Create minimal bootstrap logger for early startup
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .CreateLogger();

        Log.Information("TSM31.Dielectric WinForm host starting...");
        Log.Debug("CLR Version: {ClrVersion}, OS: {OS}",
            Environment.Version, Environment.OSVersion);

        try
        {
            // Register exception handlers with logging
            Application.ThreadException += (_, e) =>
            {
                Log.Fatal(e.Exception, "Unhandled thread exception in {Handler}",
                    nameof(Application.ThreadException));
                LogException(e.Exception, nameof(Application.ThreadException));
            };

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                Log.Fatal(exception, "Unhandled AppDomain exception (IsTerminating={IsTerminating})",
                    e.IsTerminating);
                LogException(e.ExceptionObject, nameof(AppDomain.UnhandledException));
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Log.Error(e.Exception, "Unobserved task exception");
                LogException(e.Exception, nameof(TaskScheduler.UnobservedTaskException));
                e.SetObserved();
            };

            Log.Debug("Exception handlers registered");

            ApplicationConfiguration.Initialize();
            Application.SetColorMode(SystemColorMode.System);

            Log.Information("Windows Forms application initialized");

            // ═══════════════════════════════════════════════════════════
            // PHASE 2: Load Configuration
            // ═══════════════════════════════════════════════════════════

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            Log.Information("Configuration loaded from {BasePath}", AppContext.BaseDirectory);

            // ═══════════════════════════════════════════════════════════
            // PHASE 3: Recreate Serilog with Full Configuration
            // ═══════════════════════════════════════════════════════════

            // Read PathOptions and update Serilog file path before configuring Serilog
            var pathOptions = configuration.GetSection("ApplicationPaths").Get<PathOptions>();
            if (pathOptions != null)
            {
                var logPath = Path.Combine(pathOptions.LogsDirectory, "log_.txt");

                // Ensure logs directory exists
                Directory.CreateDirectory(pathOptions.LogsDirectory);

                // Update the configuration value before Serilog reads it
                configuration["Serilog:WriteTo:0:Args:path"] = logPath;

                Log.Debug("Serilog file path configured: {LogPath}", logPath);
            }

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .CreateLogger();

            Log.Information("Serilog reconfigured from appsettings.json");

            // ═══════════════════════════════════════════════════════════
            // PHASE 4: Build Service Container
            // ═══════════════════════════════════════════════════════════

            Log.Debug("Building service container...");

            var services = new ServiceCollection();

            // Add Serilog to DI (uses the static Log.Logger we already created)
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(Log.Logger, dispose: false); // Don't dispose, we manage it
            });

            services.AddClientWindowsProjectServices();

            Services = services.BuildServiceProvider();

            Log.Information("Service container built with {ServiceCount} services", services.Count);

            // ═══════════════════════════════════════════════════════════
            // PHASE 5: Add MessageConsoleSink (now that services exist)
            // ═══════════════════════════════════════════════════════════

            Log.Debug("Configuring MessageConsoleSink...");

            var messageConsoleService = Services.GetRequiredService<MessageConsoleService>();
            var uiMinLevel = configuration.GetValue<string>("MessageConsoleUI:MinimumLevel") ?? "Information";
            var uiLogLevel = Enum.Parse<LogEventLevel>(uiMinLevel);

            // PathOptions are still valid from PHASE 3, configuration path is already updated
            // Recreate logger with MessageConsoleSink added
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.MessageConsoleSink(messageConsoleService, restrictedToMinimumLevel: uiLogLevel)
                .CreateLogger();

            Log.Information("MessageConsoleSink configured with minimum level {MinLevel}", uiLogLevel);

            // ═══════════════════════════════════════════════════════════
            // PHASE 6: Initialize State Database
            // ═══════════════════════════════════════════════════════════

            Log.Information("Initializing SQLite state database...");

            try
            {
                var initTask = Services.InitializeStateDatabaseAsync();
                initTask.Wait(); // Block until database is ready

                Log.Information("SQLite state database initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize state database");

                MessageBox.Show(
                    $"Warning: Failed to initialize state database. Session persistence will not work.\n\nError: {ex.Message}",
                    "Database Initialization Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            // ═══════════════════════════════════════════════════════════
            // PHASE 7: Create and Configure Main Form
            // ═══════════════════════════════════════════════════════════

            Log.Debug("Creating main form...");

            var form = new Form
            {
                Text = "Test Station",
                Height = 1080,
                Width = 1920,
                MinimumSize = new Size(1280, 720),
                WindowState = FormWindowState.Maximized,
                BackColor = ColorTranslator.FromHtml("#313131"),
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                KeyPreview = true
            };

            Log.Information("Main form created: {Title} ({Width}x{Height})",
                form.Text, form.Width, form.Height);

            // Handle function keys at the form level to prevent them from reaching the WebView
            form.PreviewKeyDown += (_, e) =>
            {
                // Mark function keys and Escape as input keys so they're handled by KeyDown
                if (e.KeyCode is >= Keys.F1 and <= Keys.F12 || e.KeyCode == Keys.Escape)
                {
                    e.IsInputKey = true;
                }
            };

            form.KeyDown += (_, e) =>
            {
                // Suppress function keys and Escape at the Windows Forms level
                if (e.KeyCode is >= Keys.F1 and <= Keys.F12 || e.KeyCode == Keys.Escape)
                {
                    Log.Debug("Function key suppressed at Form level: {Key}", e.KeyCode);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            // ═══════════════════════════════════════════════════════════
            // PHASE 8: Initialize BlazorWebView
            // ═══════════════════════════════════════════════════════════

            Log.Debug("Creating BlazorWebView...");

            var blazorWebView = new BlazorWebView
            {
                Dock = DockStyle.Fill,
                Services = Services,
                HostPage = "wwwroot/index.html",
                BackColor = ColorTranslator.FromHtml("#313131")
            };

            blazorWebView.WebView.DefaultBackgroundColor = ColorTranslator.FromHtml("#313131");
            blazorWebView.RootComponents.Add(new RootComponent("#app", typeof(Routes), parameters: null));

            blazorWebView.BlazorWebViewInitialized += async void (_, _) =>
            {
                try
                {
                    Log.Information("BlazorWebView initialized, starting Blazor runtime...");
                    await StartBlazor(blazorWebView);
                    Log.Information("Blazor runtime started successfully");
                }
                catch (Exception e)
                {
                    Log.Fatal(e, "Failed to start Blazor runtime");
                    LogException(e, nameof(blazorWebView.BlazorWebViewInitialized));
                }
            };

            form.Controls.Add(blazorWebView);

            Log.Debug("BlazorWebView added to form");

            form.Shown += (_, _) => { Log.Information("Main form shown to user"); };

            // ═══════════════════════════════════════════════════════════
            // PHASE 9: Register Shutdown Handler
            // ═══════════════════════════════════════════════════════════

            Application.ApplicationExit += (_, _) =>
            {
                Log.Information("Application exit requested");

                try
                {
                    // Dispose services
                    if (Services is IDisposable disposable)
                    {
                        Log.Debug("Disposing service provider...");
                        disposable.Dispose();
                    }

                    Log.Information("TSM31.Dielectric WinForm host shutdown complete");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during application shutdown");
                }
                finally
                {
                    Log.CloseAndFlush();
                }
            };

            Log.Information("Application ready, showing main form...");

            // ═══════════════════════════════════════════════════════════
            // PHASE 10: Run Application (Blocking)
            // ═══════════════════════════════════════════════════════════

            Application.Run(form);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error during application startup");

            MessageBox.Show(
                $"A fatal error occurred during startup:\n\n{ex.Message}\n\nThe application will now exit.",
                "Fatal Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Log.Information("Application main loop exited");
            Log.CloseAndFlush();
        }
    }

    private static async Task StartBlazor(BlazorWebView blazorWebView)
    {
        Log.Debug("Executing Blazor.start() script...");

        var attempts = 0;
        while (await blazorWebView.WebView.ExecuteScriptAsync("Blazor.start()") is "null")
        {
            attempts++;
            if (attempts > 100)
            {
                Log.Warning("Blazor.start() returned null after {Attempts} attempts", attempts);
                break;
            }

            await Task.Yield();
        }

        Log.Debug("Blazor.start() completed after {Attempts} attempts", attempts);

        await blazorWebView.WebView.ExecuteScriptAsync(
            "document.querySelector('body')?.classList.remove('hidden-body');");

        Log.Debug("Body visibility class removed");
    }

    private static void LogException(object? error, string reportedBy)
    {
        if (error is Exception exp)
        {
            Log.Error(exp, "Exception handled by {Reporter}", reportedBy);

            MessageBox.Show(
                $"An error occurred in {reportedBy}:\n\n{exp.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        else
        {
            var errorMessage = error?.ToString() ?? "Unknown error";
            Log.Fatal("Exception before services available: {ErrorMessage}", errorMessage);

            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static IServiceProvider? Services { get; set; }
}
