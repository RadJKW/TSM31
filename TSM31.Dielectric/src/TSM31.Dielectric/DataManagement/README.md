# TestStation.Sql Project

This project is where you implement your test-station-specific data access layer.

## Purpose

The `TestStation.Sql` project provides interfaces for data access. Each test station implementation should:

1. Define Entity Framework DbContext (if using SQL Server, SQLite, etc.)
2. Implement the `ITestDataRepository<TUnitData>` interface
3. Configure connection strings and database migrations

## Getting Started

### Option 1: SQL Server with Entity Framework Core

```csharp
// 1. Install NuGet packages
// Microsoft.EntityFrameworkCore.SqlServer
// Microsoft.EntityFrameworkCore.Tools

// 2. Create your DbContext
public class MyTestDataDbContext : DbContext
{
    public DbSet<Params> Params { get; set; }
    public DbSet<Xrefs> Xrefs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlServer("Server=YOUR_SERVER;Database=TestData;Integrated Security=True;");
    }
}

// 3. Implement ITestDataRepository
public class MyTestDataRepository : ITestDataRepository<MyUnitData>
{
    private readonly MyTestDataDbContext _dbContext;

    public MyTestDataRepository(MyTestDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MyUnitData?> DownloadUnitAsync(string serialNumber)
    {
        // Query database and map to MyUnitData
        // ...
    }

    public async Task<bool> ValidateUnitExistsAsync(string identifier)
    {
        return await _dbContext.Xrefs.AnyAsync(x => x.SerialNumber == identifier);
    }
}
```

### Option 2: Mock/In-Memory Repository (for testing)

```csharp
public class MockTestDataRepository : ITestDataRepository<DemoUnitData>
{
    public Task<DemoUnitData?> DownloadUnitAsync(string identifier)
    {
        // Return hardcoded test data
        return Task.FromResult<DemoUnitData?>(new DemoUnitData
        {
            SerialNumber = identifier,
            CatalogNumber = "TEST123",
            IsDownloaded = true
        });
    }

    public Task<bool> ValidateUnitExistsAsync(string identifier)
    {
        return Task.FromResult(true);
    }
}
```

### Option 3: REST API Repository

```csharp
public class ApiTestDataRepository : ITestDataRepository<MyUnitData>
{
    private readonly HttpClient _httpClient;

    public ApiTestDataRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MyUnitData?> DownloadUnitAsync(string identifier)
    {
        var response = await _httpClient.GetAsync($"/api/units/{identifier}");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<MyUnitData>();
    }

    public async Task<bool> ValidateUnitExistsAsync(string identifier)
    {
        var response = await _httpClient.HeadAsync($"/api/units/{identifier}");
        return response.IsSuccessStatusCode;
    }
}
```

## Registering Your Repository

In your test station's service registration (e.g., `DemoServiceCollectionExtensions.cs`):

```csharp
public static IServiceCollection AddDemoTestStation(this IServiceCollection services)
{
    // Register your DbContext (if using one)
    services.AddDbContext<MyTestDataDbContext>();

    // Register your repository
    services.AddScoped<ITestDataRepository<DemoUnitData>, DemoTestDataRepository>();

    // ... register other services
}
```

## Examples from TSM31

The TSM31 dielectric test station uses:
- SQL Server with `TestDataDbContext`
- Tables: `Params` (test parameters) and `Xrefs` (serial number cross-reference)
- Custom parsing logic to map database rows to `UnitData` model

See the TSM31.TestData project for a complete real-world example.

## Best Practices

1. **Separation of Concerns**: Keep data access separate from business logic
2. **Async Operations**: All repository methods should be async
3. **Error Handling**: Gracefully handle database connectivity issues
4. **Connection Strings**: Use configuration files (appsettings.json) for connection strings
5. **Testing**: Implement mock repositories for unit testing

## Need Help?

Refer to:
- TSM31.Dielectric.Core abstractions for base classes
- TSM31.TestData project for a complete example
- Entity Framework Core documentation: https://docs.microsoft.com/ef/core/
