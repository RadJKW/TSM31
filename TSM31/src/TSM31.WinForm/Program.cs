namespace TSM31.WinForm;

using Core;
using Core.Resources;
using Core.Services;
using Core.Services.Config;
using Core.Services.Contracts;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;
using Velopack;

public static partial class Program
{
    [STAThread, Experimental("WFO5001")]
    public static void Main()
    {
        // ═══════════════════════════════════════════════════════════
        // PHASE 0: Detect Debug Mode and Set Environment
        // ═══════════════════════════════════════════════════════════

    #if DEBUG
        // Set to Development environment when running in debug mode
        // This ensures dev paths are used (e.g., C:\TestStation_TSM31\Database)
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        }
    #endif

        // ═══════════════════════════════════════════════════════════
        // PHASE 1: Bootstrap Serilog FIRST (before anything else)
        // ═══════════════════════════════════════════════════════════

        // Create minimal bootstrap logger for early startup
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();

        Log.Information("TSM31 WinForm host starting...");
        Log.Debug("CLR Version: {ClrVersion}, OS: {OS}",
        Environment.Version, Environment.OSVersion);

        try
        {
            // Register exception handlers with logging
            Application.ThreadException += (_, e) => {
                Log.Fatal(e.Exception, "Unhandled thread exception in {Handler}",
                nameof(Application.ThreadException));
                LogException(e.Exception, nameof(Application.ThreadException));
            };

            AppDomain.CurrentDomain.UnhandledException += (_, e) => {
                var exception = e.ExceptionObject as Exception;
                Log.Fatal(exception, "Unhandled AppDomain exception (IsTerminating={IsTerminating})",
                e.IsTerminating);
                LogException(e.ExceptionObject, nameof(AppDomain.UnhandledException));
            };

            TaskScheduler.UnobservedTaskException += (_, e) => {
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

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .CreateLogger();

            Log.Information("Serilog reconfigured from appsettings.json");

            // Log environment details
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            var diagnosticPath = isDevelopment
                ? @"C:\TestStation_TSM31\diagnostic"
                : @"C:\TestStation\diagnostic";

            Directory.CreateDirectory(diagnosticPath);
            Log.Information("Running in {Environment} mode, logs at {DiagnosticPath}",
            isDevelopment ? "Development" : "Production", diagnosticPath);

            // ═══════════════════════════════════════════════════════════
            // PHASE 4: Build Service Container
            // ═══════════════════════════════════════════════════════════

            Log.Debug("Building service container...");

            var services = new ServiceCollection();

            // Add Serilog to DI (uses the static Log.Logger we already created)
            services.AddLogging(loggingBuilder => {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(Log.Logger, dispose: false);// Don't dispose, we manage it
            });

            services.AddClientWindowsProjectServices(configuration);

            Services = services.BuildServiceProvider();

            Log.Information("Service container built with {ServiceCount} services", services.Count);

            // ═══════════════════════════════════════════════════════════
            // PHASE 5: Add MessageConsoleSink (now that services exist)
            // ═══════════════════════════════════════════════════════════

            Log.Debug("Configuring MessageConsoleSink...");

            var messageConsoleService = Services.GetRequiredService<MessageConsoleService>();
            var uiMinLevel = configuration.GetValue<string>("MessageConsoleUI:MinimumLevel") ?? "Information";
            var uiLogLevel = Enum.Parse<LogEventLevel>(uiMinLevel);

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
                var initTask = ServiceCollectionExtensions.InitializeStateDatabase(Services);
                initTask.Wait();// Block until database is ready

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

            var form = new Form {
                Text = AppStrings.WindowsFormTitle,
                Height = 1080,
                Width = 1920,
                MinimumSize = new Size(1280, 720),
                WindowState = FormWindowState.Maximized,
                BackColor = ColorTranslator.FromHtml("#313131ff"),
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                KeyPreview = true
            };

            Log.Information("Main form created: {Title} ({Width}x{Height})",
            form.Text, form.Width, form.Height);

            // Handle function keys at the form level to prevent them from reaching the WebView
            form.PreviewKeyDown += (_, e) => {
                // Mark function keys and Escape as input keys so they're handled by KeyDown
                if (e.KeyCode is >= Keys.F1 and <= Keys.F12 || e.KeyCode == Keys.Escape)
                {
                    e.IsInputKey = true;
                }
            };

            form.KeyDown += (_, e) => {
                // Suppress function keys and Escape at the Windows Forms level
                // They will still be passed to Blazor via the keyboard-handler.js
                if (e.KeyCode is >= Keys.F1 and <= Keys.F12 || e.KeyCode == Keys.Escape)
                {
                    Log.Debug("Function key suppressed at Form level: {Key}", e.KeyCode);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            // Test element for WebView debugging
            var testElement = new Label {
                Text = @"TEST ELEMENT (WebView not full)",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.DarkRed,
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                Visible = false
            };
            form.Controls.Add(testElement);

            // ═══════════════════════════════════════════════════════════
            // PHASE 8: Velopack Update Check
            // ═══════════════════════════════════════════════════════════

            Log.Debug("Initializing Velopack updater...");
            VelopackApp.Build().Run();

            _ = Task.Run(async () => {
                try
                {
                    Log.Information("Checking for application updates...");
                    // check for updates here
                    Log.Debug("Update check completed");
                }
                catch (Exception exp)
                {
                    Log.Error(exp, "Failed to check for updates");
                    Services?.GetRequiredService<IExceptionHandler>().Handle(exp);
                }
            });

            // ═══════════════════════════════════════════════════════════
            // PHASE 9: Initialize BlazorWebView
            // ═══════════════════════════════════════════════════════════

            Log.Debug("Configuring WebView2 environment...");
            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--enable-notifications");

            Log.Debug("Creating BlazorWebView...");
            var blazorWebView = new BlazorWebView {
                Dock = DockStyle.Fill,
                Services = Services,
                HostPage = "wwwroot/index.html",
                BackColor = ColorTranslator.FromHtml("#313131")
            };

            blazorWebView.WebView.DefaultBackgroundColor = ColorTranslator.FromHtml("#313131");
            blazorWebView.RootComponents.Add(new RootComponent("#app", typeof(Routes), parameters: null));

            blazorWebView.BlazorWebViewInitialized += async void (_, _) => {
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

            // WebView visibility debugging
            void UpdateTestElementVisibility()
            {
                var isFull = blazorWebView.Bounds == form.ClientRectangle;
                if (isFull)
                {
                    if (testElement.Visible)
                    {
                        testElement.Visible = false;
                        testElement.SendToBack();
                    }
                }
                else
                {
                    Log.Warning("BlazorWebView bounds mismatch: WebView={WebViewBounds}, Form={FormBounds}",
                    blazorWebView.Bounds, form.ClientRectangle);
                    testElement.Visible = true;
                    testElement.BringToFront();
                }
            }

            form.Resize += (_, _) => UpdateTestElementVisibility();
            blazorWebView.SizeChanged += (_, _) => UpdateTestElementVisibility();
            form.Shown += (_, _) => {
                Log.Information("Main form shown to user");
                UpdateTestElementVisibility();
            };

            // ═══════════════════════════════════════════════════════════
            // PHASE 10: Register Shutdown Handler
            // ═══════════════════════════════════════════════════════════

            Application.ApplicationExit += (_, _) => {
                Log.Information("Application exit requested");

                try
                {
                    // Dispose services
                    if (Services is IDisposable disposable)
                    {
                        Log.Debug("Disposing service provider...");
                        disposable.Dispose();
                    }

                    Log.Information("TSM31 WinForm host shutdown complete");
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
            // PHASE 11: Run Application (Blocking)
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
        if (Services is not null && error is Exception exp)
        {
            Log.Error(exp, "Exception handled by {Reporter}", reportedBy);

            Services.GetRequiredService<IExceptionHandler>()
                .Handle(exp, parameters: new() {
                    { nameof(reportedBy), reportedBy }
                },
                displayKind: AppEnvironment.IsDevelopment()
                    ? ExceptionDisplayKind.NonInterrupting
                    : ExceptionDisplayKind.None);
        }
        else
        {
            var errorMessage = error?.ToString() ?? "Unknown error";
            Log.Fatal("Exception before services available: {ErrorMessage}", errorMessage);

            Clipboard.SetText(errorMessage);
            MessageBox.Show(errorMessage, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static IServiceProvider? Services { get; set; }
}
