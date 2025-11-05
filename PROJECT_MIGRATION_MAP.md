# TSM31 to TSM31.Dielectric Project Migration Map

**Version:** 1.0
**Date:** 2025-11-04
**Purpose:** Comprehensive mapping guide for migrating functionality from the original TSM31 project to TSM31.Dielectric

---

## Table of Contents

1. [Overview](#overview)
2. [High-Level Architecture Comparison](#high-level-architecture-comparison)
3. [Project Structure Mapping](#project-structure-mapping)
4. [Namespace and Directory Mapping](#namespace-and-directory-mapping)
5. [Key Components Mapping](#key-components-mapping)
6. [Services Migration Reference](#services-migration-reference)
7. [UI Components Migration](#ui-components-migration)
8. [Database and Persistence Layer](#database-and-persistence-layer)
9. [Models and Entities](#models-and-entities)
10. [Migration Status](#migration-status)
11. [Common Migration Patterns](#common-migration-patterns)
12. [Troubleshooting Guide](#troubleshooting-guide)

---

## Overview

### Projects Summary

| Original (TSM31) | Migrated (TSM31.Dielectric) | Purpose |
|------------------|----------------------------|---------|
| `TSM31.Core` | `TSM31.Dielectric` | Main Razor Class Library with all UI and business logic |
| `TSM31.TestData` | `TSM31.Dielectric/Database` + `TSM31.Dielectric/Testing` | Data models and SQL Server access |
| `TSM31.Web` | `TSM31.Dielectric.Web` | ASP.NET Core Blazor Web Host |
| `TSM31.WinForm` | `TSM31.Dielectric.WinForm` | Windows Forms with BlazorWebView Host |

### Key Differences

1. **Organization:** TSM31.Dielectric uses feature-based folder organization (Operator, Testing, Navigation, etc.) vs. TSM31's layer-based organization (Services, Models, UI)
2. **Consolidation:** TestData project functionality is merged into TSM31.Dielectric core library
3. **Naming:** More explicit namespace naming (e.g., `Testing.UnitData` vs. just `Models.UnitData`)
4. **Interfaces:** TSM31.Dielectric introduces more abstraction with interfaces (ITestManager, ITestDataRepository)

---

## High-Level Architecture Comparison

### TSM31 (Original) Architecture

```
TSM31/
├── src/
│   ├── TSM31.Core/              # Razor Class Library
│   │   ├── UI/                  # Blazor components
│   │   ├── Services/            # Business logic services
│   │   ├── Models/              # Application models
│   │   ├── Exceptions/          # Custom exceptions
│   │   └── Attributes/          # DI attributes
│   ├── TSM31.TestData/          # Separate data layer project
│   │   ├── Context/             # EF Core contexts
│   │   ├── Entities/            # Database entities
│   │   └── Models/              # Domain models
│   ├── TSM31.Web/               # Web host
│   └── TSM31.WinForm/           # WinForms host
```

### TSM31.Dielectric (Migrated) Architecture

```
TSM31.Dielectric/
├── src/
│   ├── TSM31.Dielectric/        # Razor Class Library (consolidated)
│   │   ├── Common/              # Shared utilities & DI registration
│   │   ├── Configuration/       # Startup & initialization
│   │   ├── Console/             # Logging infrastructure
│   │   ├── DataManagement/      # SQLite persistence + UI tabs
│   │   ├── Database/            # SQL Server access (was TSM31.TestData)
│   │   │   └── Entities/        # Param, Xref entities
│   │   ├── Navigation/          # Function key handling
│   │   ├── Operator/            # Employee management
│   │   ├── Testing/             # Test models & orchestration
│   │   ├── UI/                  # Shared Blazor components
│   │   └── Tailwind/            # CSS utilities
│   ├── TSM31.Dielectric.Web/    # Web host
│   └── TSM31.Dielectric.WinForm/# WinForms host
```

---

## Project Structure Mapping

### TSM31.Core → TSM31.Dielectric

| Original Location | New Location | Notes |
|-------------------|--------------|-------|
| `TSM31.Core/Models/` | `TSM31.Dielectric/Testing/` or appropriate feature folder | Models distributed by feature |
| `TSM31.Core/Services/` | `TSM31.Dielectric/*/` | Services distributed by feature (Operator, Testing, Navigation) |
| `TSM31.Core/Services/Persistence/` | `TSM31.Dielectric/DataManagement/` | SQLite persistence services |
| `TSM31.Core/UI/Layout/` | `TSM31.Dielectric/UI/` or `TSM31.Dielectric/DataManagement/` | UI components reorganized |
| `TSM31.Core/UI/Pages/` | `TSM31.Dielectric/DataManagement/` | Test tabs moved to DataManagement |
| `TSM31.Core/UI/Dialogs/` | `TSM31.Dielectric/Configuration/` or `TSM31.Dielectric/Operator/` | Dialogs by feature |
| `TSM31.Core/UI/Shared/` | `TSM31.Dielectric/UI/` | Shared components |
| `TSM31.Core/Exceptions/` | `TSM31.Dielectric/Common/` | (If needed) |
| `TSM31.Core/Attributes/` | `TSM31.Dielectric/Common/` | (If needed) |

### TSM31.TestData → TSM31.Dielectric

| Original Location | New Location | Notes |
|-------------------|--------------|-------|
| `TSM31.TestData/Context/TestDataDbContext.cs` | `TSM31.Dielectric/Database/TestDataDbContext.cs` | SQL Server context |
| `TSM31.TestData/Entities/Param.cs` | `TSM31.Dielectric/Database/Entities/Param.cs` | Database entity |
| `TSM31.TestData/Entities/Xref.cs` | `TSM31.Dielectric/Database/Entities/Xref.cs` | Database entity |
| `TSM31.TestData/Models/UnitData.cs` | `TSM31.Dielectric/Testing/UnitData.cs` | Domain model |
| `TSM31.TestData/Models/HipotData.cs` | `TSM31.Dielectric/Testing/HipotData.cs` | Test data model |
| `TSM31.TestData/Models/InducedData.cs` | `TSM31.Dielectric/Testing/InducedData.cs` | Test data model |
| `TSM31.TestData/Models/ImpulseData.cs` | `TSM31.Dielectric/Testing/ImpulseData.cs` | Test data model |
| `TSM31.TestData/Models/Ratings.cs` | `TSM31.Dielectric/Testing/DielectricRatings.cs` | Renamed for clarity |
| `TSM31.TestData/Models/TestStatus.cs` | `TSM31.Dielectric/Testing/TestStatus.cs` | Test status model |

---

## Namespace and Directory Mapping

### Detailed Namespace Conversion Table

| TSM31 Namespace | TSM31.Dielectric Namespace | Purpose |
|-----------------|----------------------------|---------|
| `TSM31.Core.Models` | `TSM31.Dielectric.Testing` | Test-related models |
| `TSM31.Core.Models` | `TSM31.Dielectric.Operator` | Employee/operator models |
| `TSM31.Core.Models` | `TSM31.Dielectric.Navigation` | FunctionKey models |
| `TSM31.Core.Models` | `TSM31.Dielectric.Configuration` | Startup models |
| `TSM31.Core.Models` | `TSM31.Dielectric.Common` | Shared models (TableColumnDefinition) |
| `TSM31.Core.Services` | `TSM31.Dielectric.Testing` | TestManager, ITestManager |
| `TSM31.Core.Services` | `TSM31.Dielectric.Operator` | EmployeeService, SessionManager |
| `TSM31.Core.Services` | `TSM31.Dielectric.Navigation` | FunctionKeyService |
| `TSM31.Core.Services` | `TSM31.Dielectric.Configuration` | StartupService, InitializationStateService |
| `TSM31.Core.Services` | `TSM31.Dielectric.Console` | MessageConsoleService |
| `TSM31.Core.Services.Persistence` | `TSM31.Dielectric.DataManagement` | AppStateStorageService, AppStateDbContext |
| `TSM31.TestData.Context` | `TSM31.Dielectric.Database` | TestDataDbContext |
| `TSM31.TestData.Entities` | `TSM31.Dielectric.Database.Entities` | Param, Xref |
| `TSM31.TestData.Models` | `TSM31.Dielectric.Testing` | UnitData, HipotData, InducedData, ImpulseData |
| `TSM31.Core.UI` | `TSM31.Dielectric.UI` | Shared UI components |
| `TSM31.Core.UI.Pages` | `TSM31.Dielectric.DataManagement` | Test tabs (DataEntryTab, HipotTab, etc.) |
| `TSM31.Core.UI.Dialogs` | `TSM31.Dielectric.Configuration` or `.Operator` | Feature-specific dialogs |

---

## Key Components Mapping

### 1. Test Management

| TSM31 Component | TSM31.Dielectric Component | Location Mapping |
|-----------------|----------------------------|------------------|
| `DielectricTestManager.cs` | `TestManager.cs` (concrete implementation) | `Services/` → `Testing/` |
| _(abstract/interface)_ | `ITestManager.cs` (new interface) | N/A → `Testing/` |
| `UnitDataService.cs` | `TestDataRepository.cs` (renamed & refactored) | `Services/` → `Database/` |
| _(not interfaced)_ | `ITestDataRepository<T>.cs` (new) | N/A → `Database/` |
| `TestConfigurationService.cs` | _Not yet migrated_ | `Services/` → TBD |

**Key Changes:**
- `DielectricTestManager` → `TestManager` with `ITestManager` interface
- `UnitDataService` → `TestDataRepository<UnitData>` with repository pattern
- TestManager no longer abstract, directly implements test orchestration

**Migration Example:**
```csharp
// TSM31 (Original)
public abstract class DielectricTestManager
{
    private readonly UnitDataService _unitDataService;

    public async Task<UnitData> DownloadUnitAsync(string serial)
    {
        return await _unitDataService.DownloadUnitAsync(serial);
    }
}

// TSM31.Dielectric (Migrated)
public class TestManager : ITestManager
{
    private readonly ITestDataRepository<UnitData> _repository;

    public async Task<UnitData> DownloadUnitAsync(string serial)
    {
        return await _repository.DownloadAsync(serial);
    }
}
```

---

### 2. Operator Management

| TSM31 Component | TSM31.Dielectric Component | Location Mapping |
|-----------------|----------------------------|------------------|
| `EmployeeService.cs` | `EmployeeService.cs` (refactored) | `Services/` → `Operator/` |
| `SessionManager.cs` | `SessionManager.cs` (refactored) | `Services/` → `Operator/` |
| `Employee.cs` | `Employee.cs` (same model) | `Models/` → `Operator/` |

**Key Changes:**
- Simplified authentication flow
- Better separation of session restoration logic
- Uses `RadUtils.SQLServer` for employee lookup

**Migration Example:**
```csharp
// TSM31 (Original)
namespace TSM31.Core.Services
{
    public class EmployeeService
    {
        // Implementation
    }
}

// TSM31.Dielectric (Migrated)
namespace TSM31.Dielectric.Operator
{
    public class EmployeeService
    {
        // Refactored implementation
    }
}
```

---

### 3. Navigation and Function Keys

| TSM31 Component | TSM31.Dielectric Component | Location Mapping |
|-----------------|----------------------------|------------------|
| `FunctionKeyService.cs` | `FunctionKeyService.cs` | `Services/` → `Navigation/` |
| `FunctionKey.cs` | `FunctionKey.cs` | `Models/` → `Navigation/` |

**Key Changes:**
- Dedicated Navigation namespace
- JavaScript interop for keyboard handling
- More explicit key mapping configuration

---

### 4. Configuration and Startup

| TSM31 Component | TSM31.Dielectric Component | Location Mapping |
|-----------------|----------------------------|------------------|
| `StartupService.cs` | `StartupService.cs` | `Services/` → `Configuration/` |
| `InitializationStateService.cs` | `InitializationStateService.cs` | `Services/` → `Configuration/` |
| `StartupModels.cs` | `StartupModels.cs` | `Models/` → `Configuration/` |
| _(not present)_ | `PathOptions.cs` (new) | N/A → `Configuration/` |

**New Features in TSM31.Dielectric:**
- `PathOptions` for configurable database/log paths
- Better separation of startup concerns

---

### 5. Data Persistence

| TSM31 Component | TSM31.Dielectric Component | Location Mapping |
|-----------------|----------------------------|------------------|
| `TSM31StateDbContext.cs` | `AppStateDbContext.cs` (renamed) | `Services/Persistence/` → `DataManagement/` |
| `AppStateStorageService.cs` | `AppStateStorageService.cs` | `Services/Persistence/` → `DataManagement/` |
| `OptionsStorageService.cs` | _Not yet migrated_ | `Services/` → TBD |
| `TelemetryStorageService.cs` | _Partial: TelemetryEvent exists_ | `Services/` → `DataManagement/` (partial) |

**Key Changes:**
- Renamed context to `AppStateDbContext` for clarity
- Moved to `DataManagement` namespace
- Added `IAppStateStorageService` interface

---

### 6. Logging and Console

| TSM31 Component | TSM31.Dielectric Component | Location Mapping |
|-----------------|----------------------------|------------------|
| `MessageConsoleService.cs` | `MessageConsoleService.cs` | `Services/` → `Console/` |
| _(Serilog configuration)_ | `SerilogExtensions.cs` (new) | N/A → `Console/` |
| _(Serilog sink)_ | `MessageConsoleSink.cs` (new) | N/A → `Console/` |

**Key Changes:**
- Dedicated Console namespace for logging infrastructure
- Custom Serilog sink for UI message display
- Better structured logging configuration

---

## Services Migration Reference

### Complete Services Mapping

| TSM31 Service | TSM31.Dielectric Service | Status | Notes |
|---------------|--------------------------|--------|-------|
| `DielectricTestManager` | `TestManager` | ✅ Migrated | Now concrete, implements ITestManager |
| `UnitDataService` | `TestDataRepository` | ✅ Migrated | Renamed, uses repository pattern |
| `EmployeeService` | `EmployeeService` | ✅ Migrated | Refactored, in Operator namespace |
| `FunctionKeyService` | `FunctionKeyService` | ✅ Migrated | In Navigation namespace |
| `TestConfigurationService` | _Not yet migrated_ | ⏳ Pending | |
| `OptionsStorageService` | _Not yet migrated_ | ⏳ Pending | BIL options caching |
| `TelemetryStorageService` | _Partial migration_ | ⚠️ Partial | TelemetryEvent exists, service TBD |
| `MessageConsoleService` | `MessageConsoleService` | ✅ Migrated | In Console namespace |
| `InitializationStateService` | `InitializationStateService` | ✅ Migrated | In Configuration namespace |
| `AppStateService` | _Merged into TestManager_ | ✅ Migrated | Functionality distributed |
| `StartupService` | `StartupService` | ✅ Migrated | In Configuration namespace |
| `SessionManager` | `SessionManager` | ✅ Migrated | In Operator namespace |
| `PubSubService` | _Not yet migrated_ | ⏳ Pending | Event pub/sub infrastructure |
| `AppStateStorageService` | `AppStateStorageService` | ✅ Migrated | In DataManagement namespace |

---

## UI Components Migration

### Layout Components

| TSM31 Component | TSM31.Dielectric Component | Status | Location |
|-----------------|----------------------------|--------|----------|
| `TestStationLayout.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Layout/` → TBD |
| `MainLayout.razor` | `MainLayout.razor` | ✅ Migrated | `UI/Layout/` → `UI/` |
| `NavMenu.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Layout/` → TBD |

### Page Components (Test Tabs)

| TSM31 Component | TSM31.Dielectric Component | Status | Location |
|-----------------|----------------------------|--------|----------|
| `TestStationHome.razor` | `Home.razor` | ✅ Migrated | `UI/Pages/` → `UI/` |
| `DataEntryTab.razor` | `DataEntryTab.razor` | ✅ Migrated | `UI/Pages/` → `DataManagement/` |
| `HipotTab.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Pages/` → `DataManagement/` |
| `ImpulseTab.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Pages/` → `DataManagement/` |
| `InducedTab.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Pages/` → `DataManagement/` |

### Dialog Components

| TSM31 Component | TSM31.Dielectric Component | Status | Location |
|-----------------|----------------------------|--------|----------|
| `OperatorIdDialog.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Dialogs/` → `Operator/` |
| `DownloadDialog.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Dialogs/` → `DataManagement/` |
| `SplashScreenDialog.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Dialogs/` → `Configuration/` |
| `OperatorRestoreDialog.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Dialogs/` → `Operator/` |
| `WelcomeScreen.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Dialogs/` → `UI/` |

### Shared Components

| TSM31 Component | TSM31.Dielectric Component | Status | Location |
|-----------------|----------------------------|--------|----------|
| `MenuComponent.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Shared/` → `UI/` |
| `MenuButton.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Shared/` → `UI/` |
| `TransformerDataBar.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Shared/` → `UI/` |
| `MessagesBar.razor` | _Not yet migrated_ | ⏳ Pending | `UI/Shared/` → `Console/` |
| `DataTable.razor` | `DataTable.razor` | ✅ Migrated | `UI/Shared/` → `UI/` |

---

## Database and Persistence Layer

### SQL Server Database (TestData)

| TSM31 Component | TSM31.Dielectric Component | Status |
|-----------------|----------------------------|--------|
| `TestDataDbContext.cs` | `TestDataDbContext.cs` | ✅ Migrated |
| `Param.cs` entity | `Param.cs` entity | ✅ Migrated |
| `Xref.cs` entity | `Xref.cs` entity | ✅ Migrated |

**Connection String:**
- Original: `Server=RAD-SQL;Database=TestData;Integrated Security=True;...`
- Migrated: Same (hardcoded in TestDataDbContext)

**Tables Accessed:**
- `dbo.Params` - Test parameters and specifications
- `dbo.Xref` - Serial number to catalog/work order mapping

### SQLite Database (Local State)

| TSM31 Component | TSM31.Dielectric Component | Status |
|-----------------|----------------------------|--------|
| `TSM31StateDbContext.cs` | `AppStateDbContext.cs` | ✅ Migrated (renamed) |
| `AppSessionState` entity | _Check status_ | ⚠️ Verify |
| `SessionUnit` entity | _Check status_ | ⚠️ Verify |
| `SessionRating` entity | _Check status_ | ⚠️ Verify |
| `InducedTest` entity | _Check status_ | ⚠️ Verify |
| `ImpulseTest` entity | _Check status_ | ⚠️ Verify |
| `HipotTest` entity | _Check status_ | ⚠️ Verify |
| `TelemetryTestEvent` entity | `TelemetryEvent` | ✅ Migrated (renamed) |
| `TelemetryDownloadEvent` entity | _Check status_ | ⚠️ Verify |
| `Unit` entity | _Check status_ | ⚠️ Verify |
| `Rating` entity | _Check status_ | ⚠️ Verify |
| `OptionsCacheEntity` entity | _Check status_ | ⚠️ Verify |

**Database Paths:**
- Original: Configured via application settings
- Migrated: Configurable via `PathOptions` in `Configuration/PathOptions.cs`

---

## Models and Entities

### Test Data Models

| TSM31 Model | TSM31.Dielectric Model | Status | Key Changes |
|-------------|------------------------|--------|-------------|
| `UnitData.cs` | `UnitData.cs` | ✅ Migrated | Extends `UnitDataBase`, added DielectricRatings |
| `HipotData.cs` | `HipotData.cs` | ✅ Migrated | Same structure |
| `HipotTests.cs` | `HipotTests.cs` | ✅ Migrated | Same structure |
| `InducedData.cs` | `InducedData.cs` | ✅ Migrated | Same structure |
| `InducedTests.cs` | `InducedTests.cs` | ✅ Migrated | Same structure |
| `ImpulseData.cs` | `ImpulseData.cs` | ✅ Migrated | Same structure |
| `ImpulseTests.cs` | `ImpulseTests.cs` | ✅ Migrated | Same structure |
| `Ratings.cs` | `DielectricRatings.cs` + `DielectricRatingsClass.cs` | ✅ Migrated | Split into two classes |
| `TestStatus.cs` | `TestStatus.cs` | ✅ Migrated | Same structure |
| `TestStationData.cs` | _Not yet migrated_ | ⏳ Pending | |
| `HipotCoefficients.cs` | _Check status_ | ⚠️ Verify | |
| `HipotMeterReading.cs` | _Check status_ | ⚠️ Verify | |
| `InducedCoefficients.cs` | _Check status_ | ⚠️ Verify | |

### Enums

| TSM31 Enum | TSM31.Dielectric Enum | Status | Location Change |
|------------|----------------------|--------|-----------------|
| `TransformerType` | `TransformerType` | ✅ Migrated | `Models/` → `Testing/` |
| `TestStatusType` | `TestStatusType` | ✅ Migrated | `Models/` → `Testing/` |
| `TestActions` | `TestActions` | ✅ Migrated | `Models/` → `Testing/` |
| `DialogToShow` | `DialogToShow` | ✅ Migrated | `Models/` → `Configuration/` |
| `StartupResult` | `StartupResult` | ✅ Migrated | `Models/` → `Configuration/` |

### Application Models

| TSM31 Model | TSM31.Dielectric Model | Status | Location Change |
|-------------|------------------------|--------|-----------------|
| `Employee.cs` | `Employee.cs` | ✅ Migrated | `Models/` → `Operator/` |
| `FunctionKey.cs` | `FunctionKey.cs` | ✅ Migrated | `Models/` → `Navigation/` |
| `TableColumnDefinition.cs` | `TableColumnDefinition.cs` | ✅ Migrated | `Models/` → `Common/` |
| `StartupModels.cs` | `StartupModels.cs` | ✅ Migrated | `Models/` → `Configuration/` |

---

## Migration Status

### ✅ Fully Migrated Components

1. **Database Access Layer**
   - TestDataDbContext (SQL Server)
   - Param and Xref entities
   - TestDataRepository (download logic)

2. **Core Test Models**
   - UnitData (with DielectricRatings extension)
   - HipotData and HipotTests
   - InducedData and InducedTests
   - ImpulseData and ImpulseTests
   - TestStatus

3. **Test Management**
   - TestManager (implements ITestManager)
   - Basic test orchestration logic

4. **Operator Management**
   - EmployeeService
   - SessionManager
   - Employee model

5. **Configuration**
   - StartupService
   - InitializationStateService
   - PathOptions

6. **Logging**
   - MessageConsoleService
   - Serilog integration with custom sinks

7. **Basic UI**
   - MainLayout
   - Home page
   - DataEntryTab (functional with unit download)
   - DataTable component

8. **Dependency Injection**
   - ServiceCollectionExtensions (registers all services)

---

### ⏳ Pending Migration

1. **Advanced UI Components**
   - TestStationLayout (main application shell)
   - NavMenu
   - HipotTab
   - ImpulseTab
   - InducedTab

2. **Dialogs**
   - OperatorIdDialog
   - DownloadDialog
   - SplashScreenDialog
   - OperatorRestoreDialog
   - WelcomeScreen

3. **Shared UI Components**
   - MenuComponent
   - MenuButton
   - TransformerDataBar
   - MessagesBar

4. **Services**
   - TestConfigurationService
   - OptionsStorageService (BIL caching)
   - TelemetryStorageService (complete implementation)
   - PubSubService

5. **Additional Models**
   - TestStationData
   - HipotCoefficients
   - HipotMeterReading
   - InducedCoefficients

---

### ⚠️ Needs Verification

1. **SQLite Entities**
   - Verify all entity classes migrated from TSM31StateDbContext
   - Check if entity relationships are preserved
   - Validate migrations and schema

2. **Event Handling**
   - Verify event subscriptions migrated correctly
   - Check OnUnitDataChanged, OnOperatorChanged, etc.

3. **Navigation Flow**
   - Function key handling end-to-end
   - Test action navigation state management

---

## Common Migration Patterns

### Pattern 1: Service Migration with Namespace Change

**Original (TSM31):**
```csharp
// File: TSM31.Core/Services/EmployeeService.cs
namespace TSM31.Core.Services
{
    public class EmployeeService
    {
        // Implementation
    }
}
```

**Migrated (TSM31.Dielectric):**
```csharp
// File: TSM31.Dielectric/Operator/EmployeeService.cs
namespace TSM31.Dielectric.Operator
{
    public class EmployeeService
    {
        // Implementation
    }
}
```

**Key Steps:**
1. Identify the feature area (Operator, Testing, Navigation, etc.)
2. Move file to appropriate feature folder
3. Update namespace
4. Update all using statements in dependent files
5. Update DI registration in ServiceCollectionExtensions

---

### Pattern 2: Model Migration with Consolidation

**Original (TSM31):**
```csharp
// File: TSM31.TestData/Models/UnitData.cs
namespace TSM31.TestData.Models
{
    public class UnitData
    {
        // Properties
    }
}
```

**Migrated (TSM31.Dielectric):**
```csharp
// File: TSM31.Dielectric/Testing/UnitData.cs
namespace TSM31.Dielectric.Testing
{
    public class UnitData : UnitDataBase
    {
        // Properties + Extensions
    }
}
```

**Key Steps:**
1. Move model from separate TSM31.TestData project into TSM31.Dielectric
2. Place in appropriate feature namespace (Testing)
3. Consider inheritance/composition if adding functionality
4. Update all references

---

### Pattern 3: DbContext Migration

**Original (TSM31):**
```csharp
// File: TSM31.TestData/Context/TestDataDbContext.cs
namespace TSM31.TestData.Context
{
    public class TestDataDbContext : DbContext
    {
        // Configuration
    }
}
```

**Migrated (TSM31.Dielectric):**
```csharp
// File: TSM31.Dielectric/Database/TestDataDbContext.cs
namespace TSM31.Dielectric.Database
{
    public class TestDataDbContext : DbContext
    {
        // Configuration (same)
    }
}
```

**Key Steps:**
1. Move to Database folder in core library
2. Update namespace
3. Verify connection strings still valid
4. Update entity namespaces (Database.Entities)
5. Update DI registration

---

### Pattern 4: UI Component Migration

**Original (TSM31):**
```csharp
// File: TSM31.Core/UI/Pages/DataEntryTab.razor
@page "/data-entry"
@namespace TSM31.Core.UI.Pages

// Component implementation
```

**Migrated (TSM31.Dielectric):**
```csharp
// File: TSM31.Dielectric/DataManagement/DataEntryTab.razor
@page "/data-entry"
@namespace TSM31.Dielectric.DataManagement

// Component implementation (possibly refactored)
```

**Key Steps:**
1. Determine feature area (DataManagement for test tabs)
2. Move .razor and .razor.cs files
3. Update @namespace directive
4. Update using statements
5. Update service injections if namespace changed
6. Update any @page routes if needed
7. Test component renders correctly

---

### Pattern 5: Introducing Abstractions (Interfaces)

**Original (TSM31):**
```csharp
// No interface, direct implementation
public class UnitDataService
{
    public async Task<UnitData> DownloadUnitAsync(string serial)
    {
        // Implementation
    }
}
```

**Migrated (TSM31.Dielectric):**
```csharp
// Interface added for testability
public interface ITestDataRepository<T>
{
    Task<T> DownloadAsync(string serial);
}

public class TestDataRepository : ITestDataRepository<UnitData>
{
    public async Task<UnitData> DownloadAsync(string serial)
    {
        // Implementation
    }
}
```

**Key Steps:**
1. Define interface for service contract
2. Implement interface in concrete class
3. Register both interface and implementation in DI
4. Update consuming code to depend on interface
5. Enable easier testing with mocks

---

## Troubleshooting Guide

### Issue 1: "Namespace not found" errors after migration

**Problem:** After migrating a component, other files cannot find the namespace.

**Solution:**
1. Search for all usages of the old namespace (e.g., `TSM31.Core.Services`)
2. Update using statements to new namespace (e.g., `TSM31.Dielectric.Testing`)
3. Check for fully qualified type names in code
4. Verify DI registrations updated in ServiceCollectionExtensions

**Example:**
```csharp
// Before
using TSM31.Core.Services;

// After
using TSM31.Dielectric.Testing;
```

---

### Issue 2: DI registration errors (service not found)

**Problem:** Service cannot be resolved at runtime.

**Solution:**
1. Check ServiceCollectionExtensions.cs in TSM31.Dielectric/Common/
2. Ensure service registered with correct lifetime (Scoped, Singleton, Transient)
3. Verify interface registered if using interface-based DI
4. Check constructor dependencies are all registered

**Example:**
```csharp
// TSM31.Dielectric/Common/ServiceCollectionExtensions.cs
services.AddScoped<ITestManager, TestManager>();
services.AddScoped<ITestDataRepository<UnitData>, TestDataRepository>();
```

---

### Issue 3: Database connection issues

**Problem:** Cannot connect to SQL Server or SQLite database.

**Solution:**

**For SQL Server (TestDataDbContext):**
1. Verify connection string in TestDataDbContext.cs:
   ```csharp
   optionsBuilder.UseSqlServer("Server=RAD-SQL;Database=TestData;...");
   ```
2. Check network access to SQL Server
3. Verify Integrated Security or provide credentials

**For SQLite (AppStateDbContext):**
1. Check PathOptions configuration
2. Verify database directory exists and is writable
3. Check if migrations applied (EnsureCreated or Migrate)

---

### Issue 4: UI component not rendering

**Problem:** Blazor component shows blank or errors.

**Solution:**
1. Check @namespace directive matches file location
2. Verify all @inject services are registered in DI
3. Check for null reference exceptions in OnInitialized/OnInitializedAsync
4. Verify @page route is unique and correct
5. Check browser console for JavaScript errors
6. Ensure _Imports.razor includes necessary using statements

**Example _Imports.razor:**
```razor
@using Microsoft.AspNetCore.Components
@using TSM31.Dielectric.UI
@using TSM31.Dielectric.Testing
@using TSM31.Dielectric.Operator
```

---

### Issue 5: Function key navigation not working

**Problem:** Function keys (F1-F12) don't trigger navigation.

**Solution:**
1. Verify FunctionKeyService is registered and injected
2. Check JavaScript interop is initialized
3. In WinForms host, verify PreviewKeyDown and KeyDown events hooked up
4. Check FunctionKey definitions in service
5. Verify event handlers subscribed in components

**Reference:**
- WinForms: TSM31.Dielectric.WinForm/Program.cs
- JavaScript interop: TSM31.Dielectric/Navigation/FunctionKeyService.cs

---

### Issue 6: Events not firing between components

**Problem:** Component events (OnUnitDataChanged, OnOperatorChanged) not received.

**Solution:**
1. Verify event subscription syntax:
   ```csharp
   testManager.OnUnitDataChanged += HandleUnitDataChanged;
   ```
2. Unsubscribe in Dispose to prevent memory leaks:
   ```csharp
   public void Dispose()
   {
       testManager.OnUnitDataChanged -= HandleUnitDataChanged;
   }
   ```
3. Check event is actually invoked in source:
   ```csharp
   OnUnitDataChanged?.Invoke(this, unitData);
   ```
4. Verify both components use same service instance (check DI lifetime)

---

### Issue 7: Finding equivalent implementation

**Problem:** Need to find how something was implemented in TSM31 to fix in TSM31.Dielectric.

**Solution:**

1. **Use this mapping document** - Search for the component name
2. **Search by file name:**
   ```bash
   # Find in original
   find TSM31/ -name "*ComponentName*"

   # Find in migrated
   find TSM31.Dielectric/ -name "*ComponentName*"
   ```

3. **Search by class/interface name:**
   ```bash
   grep -r "class ComponentName" TSM31/
   grep -r "class ComponentName" TSM31.Dielectric/
   ```

4. **Search by namespace:**
   ```bash
   grep -r "namespace TSM31.Core.Services" TSM31/
   ```

5. **Compare implementations:**
   ```bash
   # After finding both files
   diff TSM31/path/to/file.cs TSM31.Dielectric/path/to/file.cs
   ```

6. **Check git history for migration:**
   ```bash
   git log --all --follow -- TSM31.Dielectric/path/to/file.cs
   ```

---

## Quick Reference: Finding Components

### By Feature Area

| Need to find... | Look in TSM31 | Look in TSM31.Dielectric |
|-----------------|---------------|--------------------------|
| Test orchestration | `TSM31.Core/Services/DielectricTestManager.cs` | `TSM31.Dielectric/Testing/TestManager.cs` |
| Unit download | `TSM31.Core/Services/UnitDataService.cs` | `TSM31.Dielectric/Database/TestDataRepository.cs` |
| Operator login | `TSM31.Core/Services/EmployeeService.cs` | `TSM31.Dielectric/Operator/EmployeeService.cs` |
| Function keys | `TSM31.Core/Services/FunctionKeyService.cs` | `TSM31.Dielectric/Navigation/FunctionKeyService.cs` |
| Startup logic | `TSM31.Core/Services/StartupService.cs` | `TSM31.Dielectric/Configuration/StartupService.cs` |
| SQLite persistence | `TSM31.Core/Services/Persistence/AppStateStorageService.cs` | `TSM31.Dielectric/DataManagement/AppStateStorageService.cs` |
| SQL Server access | `TSM31.TestData/Context/TestDataDbContext.cs` | `TSM31.Dielectric/Database/TestDataDbContext.cs` |
| Test data models | `TSM31.TestData/Models/UnitData.cs` | `TSM31.Dielectric/Testing/UnitData.cs` |
| Data entry UI | `TSM31.Core/UI/Pages/DataEntryTab.razor` | `TSM31.Dielectric/DataManagement/DataEntryTab.razor` |
| Logging | `TSM31.Core/Services/MessageConsoleService.cs` | `TSM31.Dielectric/Console/MessageConsoleService.cs` |

---

### By File Type

| File Type | TSM31 Location | TSM31.Dielectric Location |
|-----------|----------------|---------------------------|
| Services | `TSM31.Core/Services/` | `TSM31.Dielectric/[Feature]/` (distributed) |
| Models | `TSM31.Core/Models/` or `TSM31.TestData/Models/` | `TSM31.Dielectric/[Feature]/` (distributed) |
| UI Components | `TSM31.Core/UI/` | `TSM31.Dielectric/UI/` or `TSM31.Dielectric/DataManagement/` |
| DbContexts | `TSM31.Core/Services/Persistence/` or `TSM31.TestData/Context/` | `TSM31.Dielectric/Database/` or `TSM31.Dielectric/DataManagement/` |
| Entities | `TSM31.TestData/Entities/` | `TSM31.Dielectric/Database/Entities/` |
| DI Registration | `TSM31.Core/ServiceCollectionExtensions.cs` (if exists) | `TSM31.Dielectric/Common/ServiceCollectionExtensions.cs` |

---

## Migration Workflow Recommendations

### When migrating a new component from TSM31 to TSM31.Dielectric:

1. **Identify Feature Area**
   - Determine which feature area the component belongs to (Testing, Operator, Navigation, etc.)
   - Consult "Namespace and Directory Mapping" section above

2. **Check Dependencies**
   - List all services, models, and other components the component depends on
   - Verify all dependencies are already migrated
   - If not, migrate dependencies first (bottom-up approach)

3. **Create Component File**
   - Create file in appropriate feature folder in TSM31.Dielectric
   - Update namespace to match folder structure
   - Copy implementation from TSM31

4. **Update Namespaces and Using Statements**
   - Update all using statements to new namespaces
   - Update any fully qualified type names
   - Fix any broken references

5. **Refactor If Needed**
   - Consider introducing interfaces for better testability
   - Apply any architectural improvements
   - Update naming conventions if needed

6. **Register in DI**
   - Add service registration in ServiceCollectionExtensions.cs
   - Choose appropriate lifetime (Scoped, Singleton, Transient)
   - Register interface if created

7. **Update Consumers**
   - Find all components that use this component
   - Update their using statements
   - Update constructor injection if interface added

8. **Test**
   - Build both Web and WinForms projects
   - Run application and test functionality
   - Verify no regressions

9. **Document**
   - Update this mapping document with migration status
   - Note any significant changes or gotchas
   - Update "Migration Status" section

---

## Conclusion

This mapping document serves as a comprehensive reference for understanding the relationship between TSM31 and TSM31.Dielectric projects. Use it to:

- **Locate equivalent components** when fixing bugs or adding features
- **Understand architectural changes** between the two projects
- **Guide migration efforts** for remaining components
- **Troubleshoot issues** by finding original implementations
- **Maintain consistency** in naming and structure

For questions or updates to this document, please contact the development team.

---

**Document Version:** 1.0
**Last Updated:** 2025-11-04
**Maintainer:** Development Team
