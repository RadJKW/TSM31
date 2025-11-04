# Blazor Hybrid Test Station Application

## Overview
This is a keyboard-navigatable Blazor Hybrid application designed for electrical transformer testing. The application runs on Windows Forms with a full-screen interface optimized for 1920x1080 resolution.

## Architecture

### Project Structure
- **TSM31.Core**: Contains all UI components, layouts, models, and services
- **TSM31.WinForm**: Windows Forms host application
- **TSM31.Web**: Web version (optional)

### Key Components

#### Layout
- **TestStationLayout.razor**: Main application layout with:
  - Unit data header bar
  - Left sidebar with menu and test power control
  - Main content area
  - Status bar footer

#### Services
- **KeyboardNavigationService**: Manages keyboard navigation state and events
  - Tracks current tab and sub-tab
  - Handles test power state
  - Provides events for UI updates

#### Main Components
1. **MenuComponent**: Dynamic menu showing available keyboard shortcuts
2. **WelcomeScreen**: Initial landing page with navigation guide
3. **DataEntryTab**: Transformer data input form
4. **HipotTab**: Hipot test interface with meter displays
5. **ImpulseTab**: Impulse test interface with generator controls
6. **InducedTab**: Induced test interface with multi-channel meter display
7. **SplashScreenDialog**: Initialization splash screen
8. **OperatorIdDialog**: Operator ID entry dialog

### Keyboard Navigation

#### Main Menu (F-Keys)
- **ESC**: Cancel/Return to menu
- **F1**: Data Entry
- **F2**: First Induced Test
- **F3**: Hipot Test
- **F4**: Impulse Test
- **F5**: Second Induced Test
- **F6**: Auto Test
- **F7**: Enter Operator ID
- **F8**: Upload Data
- **F9**: Re-Print Fail Tag
- **F10**: Next Test

#### Sub-Menus
Each test tab has its own sub-menu accessible via F-keys when that test is active.

### Data Models

#### TransformerData
- Serial number, catalog number, unit type
- Primary and secondary specifications
- KVA rating, work order, customer info

#### Test Data Models
- **HipotTestData**: Voltage, current, time, status for hipot tests
- **ImpulseTestData**: Measurements and status for impulse tests
- **InducedTestData**: Voltage, current, power, time for induced tests

## Design Principles

### UI/UX
- **Compact Design**: Maximizes visible data on 1920x1080 screens
- **Keyboard First**: All functionality accessible via keyboard
- **Visual Feedback**: Selected tabs and buttons are highlighted
- **Status Indicators**: Real-time test status and system state

### FluentUI Integration
- Uses Microsoft FluentUI components throughout
- Minimal custom CSS
- Responsive grid layouts
- Fluent Design System tokens for theming

### State Management
- **KeyboardNavigationService**: Centralized navigation state
- Component-level state for forms and test data
- Event-driven updates between components

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Windows 10/11
- Visual Studio 2022 or VS Code

### Running the Application
```powershell
# Navigate to the WinForm project
cd TSM31.WinForm

# Run the application
dotnet run
```

### Development
1. All UI development should be done in the **TSM31.Core** project
2. The WinForm project only handles:
   - Application hosting
   - Keyboard event forwarding (if needed)
   - Native Windows integration
3. Use FluentUI components wherever possible
4. Follow the existing pattern for adding new tabs or dialogs

## Keyboard Event Handling

The application captures keyboard events at the layout level using Blazor's `@onkeydown` event handler. Function keys and Escape are handled directly in the TestStationLayout component.

### Adding New Keyboard Shortcuts
1. Update the `HandleKeyDown` method in `TestStationLayout.razor`
2. Add corresponding menu item in `MenuComponent.razor`
3. Update the `MainTab` enum if adding a new main tab

## Extending the Application

### Adding a New Test Tab
1. Create a new component in `TSM31.Core/Components/`
2. Add the tab to `MainTab` enum in `KeyboardNavigationService.cs`
3. Add navigation case in `TestStationLayout.razor`
4. Add menu item in `MenuComponent.razor`
5. Add route handling in `TestStationHome.razor`

### Adding a New Dialog
1. Create component in `TSM31.Core/Components/`
2. Use `@bind-Hidden` with FluentDialog
3. Expose `Show()` method
4. Add reference in parent component
5. Call `Show()` when needed

## Testing Considerations

### Test Data
Sample test data is initialized in each tab component's `@code` block. Replace with actual data service calls when integrating with hardware.

### Hardware Integration
The application is designed to integrate with:
- Yokogawa power meters
- High voltage generators
- Impulse generators
- Safety interlock systems

Integration points are marked with TODO comments in the code.

## Future Enhancements

1. **Auto Test Sequence**: Automated test sequence execution
2. **Data Upload**: Integration with central database
3. **Report Generation**: PDF generation for test results
4. **Print Dialogs**: Fail tag printing integration
5. **I/O Control Panel**: Direct hardware control interface
6. **Variac Timing**: Real-time TTFS (Time To Full Scale) tracking
7. **Set Condition Helpers**: Voltage range selection dialogs
8. **Meter Calibration**: Real-time meter reading integration

## Notes

- **Splash Screen Behavior**: The application shows an initialization splash screen on fresh start, but intelligently skips it during hot reloads and browser refreshes to improve development experience. This is managed by `InitializationStateService` using localStorage persistence.
- Test power control is locked to prevent accidental activation (Write Disabled)
- All test data tables use FluentDataGrid with proper type parameters
- Dialog components use `@bind-Hidden` for show/hide functionality
- The layout is centered on ultrawide monitors (3440x1440+)
- **Mouse + Keyboard**: Both keyboard shortcuts (F-keys) and mouse clicks work for navigation

## License
[Your License Here]

## Contributors
[Your Team/Contributors Here]
