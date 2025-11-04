namespace TSM31.Core.Services.Config;

public static class AppEnvironment
{
    private static readonly string Development = nameof(Development);
    private static readonly string Test = nameof(Test);
    private static readonly string Staging = nameof(Staging);
    private static readonly string Production = nameof(Production);

    public static string Current { get; private set; } =
#if Development // dotnet publish -c Debug
        Development;
#elif Test // dotnet publish -c Release -p:Environment=Test
        Test;
#elif Staging // dotnet publish -c Release -p:Environment=Staging
        Staging;
#else // dotnet publish -c Release
        Production;
#endif

    public static bool IsDevelopment()
    {
        return Is(Development);
    }

    public static bool IsTest()
    {
        return Is(Test);
    }

    public static bool IsStaging()
    {
        return Is(Staging);
    }

    public static bool IsProduction()
    {
        return Is(Production);
    }

    public static bool Is(string name)
    {
        return Current == name;
    }

    public static void Set(string name)
    {
        Current = name;
    }
}
