namespace TSM31.Dielectric.WinForm;

using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shared.Abstractions;

public static partial class Program
{
    private static void AddClientWindowsProjectServices(this IServiceCollection services)
    {
        // Add core services
        // Add all test station services
        services.AddTestStationServices();

        // Add test station manager implementation
        services.AddScoped<ITestManager, App.TestManager>();

        // Add Windows Forms specific services
        services.AddWindowsFormsBlazorWebView();
    }
}
