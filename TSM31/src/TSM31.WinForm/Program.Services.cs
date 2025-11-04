namespace TSM31.WinForm;

using Core.Services;
using Core.Services.Config;
using Core.Services.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services;

public static partial class Program
{
    private static void AddClientWindowsProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration;// Marking configuration parameter as intentionally unused

        // Add shared services (FluentUI, KeyboardNavigation, Localization, etc.)
        services.AddCoreServices();

        // Add platform-specific services
        services.AddSingleton<IExceptionHandler, WindowsExceptionHandler>();
        services.AddSingleton<IFormFactor, FormFactor>();

        // Add Windows Forms specific services
        services.AddWindowsFormsBlazorWebView();
    #if DEBUG
        services.AddBlazorWebViewDeveloperTools();

    #endif
    }
}
