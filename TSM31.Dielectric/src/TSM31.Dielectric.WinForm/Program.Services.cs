using TSM31.Dielectric.Common;
using TSM31.Dielectric.Testing;

namespace TSM31.Dielectric.WinForm;

using Microsoft.Extensions.DependencyInjection;

public static partial class Program
{
    private static void AddClientWindowsProjectServices(this IServiceCollection services)
    {
        // Add core services
        // Add all test station services
        services.AddTestStationServices();

        // Add test station manager implementation
        services.AddScoped<ITestManager, TestManager>();

        // Add Windows Forms specific services
        services.AddWindowsFormsBlazorWebView();
    }
}
