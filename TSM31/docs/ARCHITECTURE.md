# Application Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Large Pole Dielectric Console                     │
│                         (Blazor Hybrid App)                          │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ TestStationLayout.razor (Main Layout)                               │
├─────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │  Unit Data Bar (Header)                                         │ │
│ │  Serial# | Catalog# | Unit Type | KVA | Work Order | Customer  │ │
│ └─────────────────────────────────────────────────────────────────┘ │
│                                                                       │
│ ┌──────────────┬───────────────────────────────────────────────────┐ │
│ │              │                                                   │ │
│ │   Menu       │                                                   │ │
│ │  Component   │          Main Content Area                       │ │
│ │              │                                                   │ │
│ │  [ESC]Cancel │      ┌─────────────────────────────────┐         │ │
│ │  [F1] Data   │      │  TestStationHome.razor          │         │ │
│ │  [F2] First  │      │  (Route: "/")                   │         │ │
│ │  [F3] Hipot  │      │                                 │         │ │
│ │  [F4] Impulse│      │  Conditional Content:           │         │ │
│ │  [F5] Second │      │  - WelcomeScreen                │         │ │
│ │  [F6] Auto   │      │  - DataEntryTab                 │         │ │
│ │  [F7] OpID   │      │  - HipotTab                     │         │ │
│ │  [F8] Upload │      │  - ImpulseTab                   │         │ │
│ │  [F9] RePrint│      │  - InducedTab                   │         │ │
│ │  [F10] Next  │      │                                 │         │ │
│ │              │      └─────────────────────────────────┘         │ │
│ │──────────────│                                                   │ │
│ │ Test Power   │                                                   │ │
│ │  ┌────────┐  │                                                   │ │
│ │  │   ON   │  │                                                   │ │
│ │  │  (OFF) │  │                                                   │ │
│ │  └────────┘  │                                                   │ │
│ │ WriteDisabled│                                                   │ │
│ └──────────────┴───────────────────────────────────────────────────┘ │
│                                                                       │
│ ┌─────────────────────────────────────────────────────────────────┐ │
│ │ Status Bar (Footer)                                             │ │
│ │ Params Up-To-Date | METER STATUS | Date | Time | Version       │ │
│ └─────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

## Service Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Application Services                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  KeyboardNavigationService (Singleton)                           │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ • CurrentMainTab: MainTab                                   │ │
│  │ • CurrentSubTab: string                                     │ │
│  │ • TestPowerOn: bool                                         │ │
│  │                                                             │ │
│  │ Events:                                                     │ │
│  │ • OnKeyPressed(string key)                                 │ │
│  │ • OnMainTabChanged(MainTab tab)                            │ │
│  │ • OnSubTabChanged(string tab)                              │ │
│  │                                                             │ │
│  │ Methods:                                                    │ │
│  │ • HandleKeyPress(string key)                               │ │
│  │ • SetMainTab(MainTab tab)                                  │ │
│  │ • SetSubTab(string tab)                                    │ │
│  │ • ToggleTestPower()                                        │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                   │
│  IFormFactor (Platform Detection)                                │
│  IExceptionHandler (Error Handling)                              │
│  PubSubService (Future: Event Bus)                               │
└─────────────────────────────────────────────────────────────────┘
```

## Component Hierarchy

```
Routes.razor
  └─ TestStationLayout.razor (Layout)
       ├─ MenuComponent.razor (Left Sidebar)
       │    └─ MenuButton.razor (x N)
       │
       ├─ Test Power Control (Built-in)
       │
       ├─ TestStationHome.razor (Body - Route: "/")
       │    ├─ WelcomeScreen.razor (Tab: None)
       │    ├─ DataEntryTab.razor (Tab: DataEntry)
       │    ├─ HipotTab.razor (Tab: Hipot)
       │    ├─ ImpulseTab.razor (Tab: Impulse)
       │    └─ InducedTab.razor (Tab: FirstInduced | SecondInduced)
       │
       ├─ SplashScreenDialog.razor (Startup)
       └─ OperatorIdDialog.razor (On-demand)
```

## Data Flow

```
┌──────────────┐    Keyboard     ┌────────────────────┐
│  Windows     │─────Event───────▶│ TestStationLayout  │
│  Form Host   │   (@onkeydown)   │     .razor         │
└──────────────┘                  └────────────────────┘
                                           │
                                           │ HandleKeyPress
                                           ▼
                                  ┌────────────────────┐
                                  │ KeyboardNavigation │
                                  │     Service        │
                                  └────────────────────┘
                                           │
                          ┌────────────────┼────────────────┐
                          │                │                │
                   OnMainTabChanged  OnSubTabChanged  OnKeyPressed
                          │                │                │
                          ▼                ▼                ▼
              ┌──────────────────┐  ┌──────────────┐  ┌──────────┐
              │  MenuComponent   │  │  Tab         │  │ Other    │
              │  StateHasChanged │  │  Components  │  │ Listeners│
              └──────────────────┘  └──────────────┘  └──────────┘
```

## Tab Navigation State Machine

```
┌─────────────┐
│    None     │◄─── ESC from any state
│ (Welcome)   │
└─────────────┘
      │
      │ F1-F10
      ▼
┌─────────────┐     F1      ┌─────────────┐
│  DataEntry  │────────────▶│ SubTab:     │
└─────────────┘             │ DataReview  │
                            │ DataEntry   │
                            │ Hipot       │
                            │ Impulse     │
                            │ Induced     │
                            └─────────────┘

┌─────────────┐     F1-F4   ┌─────────────┐
│    Hipot    │────────────▶│ SubTab:     │
└─────────────┘             │ Simultaneous│
                            │ Primary     │
                            │ Secondary   │
                            │ 4 LVB       │
                            └─────────────┘

┌─────────────┐     F1-F4   ┌─────────────┐
│   Impulse   │────────────▶│ SubTab:     │
└─────────────┘             │ H1          │
                            │ H2          │
                            │ X1          │
                            │ X2          │
                            └─────────────┘

┌─────────────┐     F1-F5   ┌─────────────┐
│   Induced   │────────────▶│ SubTab:     │
└─────────────┘             │ DataEntry   │
                            │ FirstInduced│
                            │ Hipot       │
                            │ Impulse     │
                            │ SecondInduced│
                            └─────────────┘
```

## Project Structure

```
MauiBlazor Solution
│
├── MauiBlazor.Shared (Razor Class Library)
│   ├── Components/
│   │   ├── DataEntryTab.razor ─────────┐
│   │   ├── HipotTab.razor              │
│   │   ├── ImpulseTab.razor            ├─ Test Tabs
│   │   ├── InducedTab.razor            │
│   │   ├── WelcomeScreen.razor ────────┘
│   │   ├── MenuComponent.razor ────────┐
│   │   ├── MenuButton.razor            ├─ Navigation
│   │   ├── SplashScreenDialog.razor    │
│   │   └── OperatorIdDialog.razor ─────┘
│   ├── Layout/
│   │   ├── TestStationLayout.razor ──── Main Layout (Active)
│   │   └── MainLayout.razor ──────────── Old Layout (Unused)
│   ├── Models/
│   │   └── TestModels.cs ──────────────── Data Models
│   ├── Pages/
│   │   ├── TestStationHome.razor ──────── Main Page ("/")
│   │   ├── Home.razor ─────────────────── Old Page ("/old-home")
│   │   ├── Counter.razor ──────────────── Demo
│   │   └── Weather.razor ──────────────── Demo
│   ├── Services/
│   │   ├── KeyboardNavigationService.cs ─ Core Service
│   │   ├── IFormFactor.cs
│   │   └── Contracts/
│   ├── wwwroot/
│   │   └── app.css ────────────────────── Custom Styles
│   ├── Routes.razor ───────────────────── Router Config
│   └── _Imports.razor ─────────────────── Global Imports
│
├── MauiBlazor.WinForm (Windows Forms Host)
│   ├── Program.cs ─────────────────────── Entry Point
│   ├── Program.Services.cs ────────────── DI Configuration
│   ├── Services/
│   │   ├── FormFactor.cs
│   │   └── WindowsExceptionHandler.cs
│   └── wwwroot/
│       └── index.html ─────────────────── WebView Host Page
│
├── MauiBlazor.Web (ASP.NET Core Host)
│   └── Program.cs ─────────────────────── Web Entry Point
│
└── MauiBlazor (MAUI App)
    └── [Cross-platform mobile support]
```

## Technology Stack

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Razor Components (.razor)                           │   │
│  │  • FluentUI Components                               │   │
│  │  • Data Binding (@bind-*)                            │   │
│  │  • Event Handling (@on*)                             │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ▲
                            │
┌─────────────────────────────────────────────────────────────┐
│                      Service Layer                           │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  KeyboardNavigationService                           │   │
│  │  • State Management                                  │   │
│  │  • Event Publishing                                  │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ▲
                            │
┌─────────────────────────────────────────────────────────────┐
│                       Host Platform                          │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Windows Forms (Primary)                             │   │
│  │  • BlazorWebView                                     │   │
│  │  • Keyboard Integration                              │   │
│  │  • Native Windows Features                           │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  ASP.NET Core (Web)                                  │   │
│  │  • HTTP Server                                       │   │
│  │  • SignalR (Future)                                  │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ▲
                            │
┌─────────────────────────────────────────────────────────────┐
│                    .NET 9 Runtime                            │
│  • Blazor Framework                                          │
│  • Dependency Injection                                      │
│  • Configuration                                             │
│  • Logging                                                   │
└─────────────────────────────────────────────────────────────┘
```

## Future Integration Points

```
┌─────────────────────────────────────────────────────────────┐
│                  Application Layer                           │
│              (Current Implementation)                        │
└─────────────────────────────────────────────────────────────┘
                            │
            ┌───────────────┼───────────────┐
            ▼               ▼               ▼
┌──────────────────┐ ┌──────────────┐ ┌──────────────┐
│  Hardware        │ │  Database    │ │  External    │
│  Integration     │ │  Layer       │ │  Services    │
│  (Future)        │ │  (Future)    │ │  (Future)    │
├──────────────────┤ ├──────────────┤ ├──────────────┤
│ • Yokogawa Meter │ │ • SQL Server │ │ • Print      │
│ • HV Generator   │ │ • EF Core    │ │ • Reports    │
│ • Impulse Gen    │ │ • Repository │ │ • Upload     │
│ • I/O Control    │ │ • Migrations │ │ • Licensing  │
│ • Safety Systems │ │ • Backup     │ │ • Updates    │
└──────────────────┘ └──────────────┘ └──────────────┘
```

---

**Legend:**
- `┌─┐ └─┘` = Container/Component
- `─────▶` = Data/Event Flow
- `│` = Hierarchy/Containment
- `▼` = Direction of Flow

**Version**: 00.00.00
**Last Updated**: October 1, 2025
