# Thoughts and Notes on Development

## DielectricTestManager
This is the main class for managing the application. It handles: 
- Application state
- Configuration
- Test execution
- Communication with external devices
- Logging
- User interface updates
- Error handling
- Resource management
- Extensibility
- Data persistence
- Configures Test Station from settings files.
- etc...

### 10-08-2025
TestManager is Instantiated with default values ( on Application boot ). 
Because each razor component has methods that are called when loaded and unloaded ( OnInitializedAsync(), DisposeAsync() ) , We can then alter the TestManager in these methods to configure the TestManager as needed for each component.
