// Copyright (c) Microsoft.All rights reserved.
// Licensed under the MIT License.

namespace TSM31.Core.Services;

using Core.Models;
using Models;
using TestData.Models;

/// <summary>
/// Manager of Dielectric Testing. Global Handler for Operator Login, Unit Data Download, Test Configuration, Test Execution, and Test Data Upload.
/// Acts as a façade over lower-level services (UnitDataService, KeyboardNavigationService, TestConfigurationService)
/// so UI components only depend on this single state manager.
/// Provides automatic session persistence and recovery for crash/power loss scenarios.
/// </summary>
public class DielectricTestManager
{
    // Public API ---------------------------------------------------------------------

    // Underlying services (internal) -------------------------------------------------
    private readonly UnitDataService _unitDataService;
    private readonly TestConfigurationService _testConfigurationService;
    private readonly ITelemetryStorageService _telemetryService;
    private readonly OptionsStorageService _optionsStorageService;
    private readonly MessageConsoleService _messageConsole;
    private readonly EmployeeService _employeeService;
    private readonly Persistence.AppStateStorageService _stateStorage;

    // Events (re-exposed) -----------------------------------------------------

    public event Action? OnTestActionChanged;
    public event Action? OnUnitDataChanged;
    public event Action? OnDownloadStarted;// Raised when a unit download begins
    public event Action<bool>? OnDownloadCompleted;// Raised when a unit download completes (success flag)
    public event Action? OnPendingSerialChanged;// Raised when pending serial number changes (for menu refresh)
    public event Action? OnShowOperatorDialog;// Raised when operator dialog should be shown

    // Forward operator change events from EmployeeService
    public event Action? OnOperatorChanged
    {
        add => _employeeService.OnOperatorChanged += value;
        remove => _employeeService.OnOperatorChanged -= value;
    }

    // State -------------------------------------------------------------------
    public TestActions CurrentTestActions { get; set; } = TestActions.DataReview;
    public UnitData? CurrentUnit => _unitDataService.CurrentUnit;

    // Pass-through properties for operator state (EmployeeService is single source of truth)
    public Employee? CurrentOperator => _employeeService.Operator;
    public bool IsOperatorLoggedIn => _employeeService.Operator != null;

    // Expose EmployeeService for session restoration


    public TestStationData TestStationData { get; set; } = new();


    internal string? PendingSerialNumber { get; private set; }

    // Context-specific command handlers (set by active components) -------------------

    /// <summary>
    /// Handler for F1 key in DataEntry context. Set by DataEntryTab component.
    /// </summary>
    public Action? DataEntryF1Handler { get; set; }

    /// <summary>
    /// Handler for F9 key in DataEntry context. Set by DataEntryTab component.
    /// </summary>
    public Action? DataEntryF9Handler { get; set; }

    // Constructor --------------------------------------------------------------------
    public DielectricTestManager(
        UnitDataService unitDataService,
        TestConfigurationService testConfigurationService,
        ITelemetryStorageService telemetryService,
        OptionsStorageService optionsStorageService,
        MessageConsoleService messageConsole,
        EmployeeService employeeService,
        Persistence.AppStateStorageService stateStorage)
    {
        _unitDataService = unitDataService;
        _testConfigurationService = testConfigurationService;
        _telemetryService = telemetryService;
        _optionsStorageService = optionsStorageService;
        _messageConsole = messageConsole;
        _employeeService = employeeService;
        _stateStorage = stateStorage;

        _unitDataService.OnUnitDataChanged += () => OnUnitDataChanged?.Invoke();
    }

    // Public façade methods ---------------------------------------------------------
    public Task<(bool Success, string Message)> DownloadUnitAsync(string serialNumber, string? catalogNumber = null)
        => InternalDownloadUnitAsync(serialNumber, catalogNumber);

    public async Task ClearUnitAsync()
    {
        _unitDataService.ClearUnit();

        // Clear unit data from persisted session (keep operator logged in) - must complete before returning
        await _stateStorage.UpdateUnitDataAsync(null);
    }

    /// <summary>
    /// Legacy synchronous clear method for backward compatibility.
    /// Consider using ClearUnitAsync instead.
    /// </summary>
    public void ClearUnit()
    {
        _ = ClearUnitAsync();// Fire and forget
    }

    public void UpdatePendingSerial(string? serial)
    {
        PendingSerialNumber = serial;
        OnPendingSerialChanged?.Invoke();
    }

    /// <summary>
    /// Records a test execution event to telemetry storage.
    /// Call this from UI components when tests complete, fail, or abort.
    /// </summary>
    public async Task RecordTestExecutionAsync(string testType, int testNumber, string status, string? notes = null)
    {
        if (CurrentUnit == null) return;

        await _telemetryService.RecordTestExecutionAsync(
        CurrentUnit.SerialNumber,
        testType,
        testNumber,
        status,
        CurrentOperator?.Id,
        notes);
    }


    // Helpers -----------------------------------------------------------------------

    /// <summary>
    /// Checks if a test has any required test types (Hipot, Impulse, or Induced).
    /// </summary>
    private bool IsTestRequired(int testNumber)
    {
        if (CurrentUnit == null || testNumber < 1 || testNumber > CurrentUnit.TotalTests)
            return false;

        var testIndex = testNumber - 1;

        // Check Hipot requirements
        var hipot = CurrentUnit.Hipot.ElementAtOrDefault(testIndex);
        if (hipot != null &&
            (hipot.PrimaryStatus.Status == TestStatusType.Required ||
             hipot.SecondaryStatus.Status == TestStatusType.Required ||
             hipot.FourLvbStatus.Status == TestStatusType.Required))
        {
            return true;
        }

        // Check Induced requirements
        var induced = CurrentUnit.Induced.ElementAtOrDefault(testIndex);
        if (induced != null &&
            (induced.FirstStatus.Status == TestStatusType.Required ||
             induced.SecondStatus.Status == TestStatusType.Required))
        {
            return true;
        }

        // Check Impulse requirements
        var impulse = CurrentUnit.Impulse.ElementAtOrDefault(testIndex);
        if (impulse != null &&
            (impulse.H1Status.Status == TestStatusType.Required ||
             impulse.H2Status.Status == TestStatusType.Required ||
             impulse.H3Status.Status == TestStatusType.Required ||
             impulse.X1Status.Status == TestStatusType.Required ||
             impulse.X2Status.Status == TestStatusType.Required))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets CurrentTest to the first test that has any required tests.
    /// If test 1 has no required tests, advances to the first test that does.
    /// </summary>
    private void SetInitialCurrentTest()
    {
        if (CurrentUnit == null)
            return;

        // Check if test 1 is required
        if (IsTestRequired(1))
        {
            CurrentUnit.CurrentTest = 1;
            return;
        }

        // Find first test with requirements
        for (int i = 2; i <= CurrentUnit.TotalTests; i++)
        {
            if (IsTestRequired(i))
            {
                CurrentUnit.CurrentTest = i;
                _messageConsole.AddInfo($"Test 1 has no required tests. Auto-advancing to Test {i}", "Download");
                return;
            }
        }

        // No required tests found, default to test 1
        CurrentUnit.CurrentTest = 1;
    }

    private async Task<(bool Success, string Message)> InternalDownloadUnitAsync(string serialNumber,
                                                                                 string? catalogNumber)
    {
        // Notify UI that download is starting (for progress overlay)
        OnDownloadStarted?.Invoke();
        _messageConsole.AddInfo($"Downloading unit {serialNumber}...", "Download");

        try
        {
            var (success, message) = await _unitDataService.DownloadUnitAsync(serialNumber, catalogNumber);

            // Record telemetry for download attempt
            await _telemetryService.RecordDownloadAsync(
            serialNumber,
            catalogNumber ?? CurrentUnit?.CatalogNumber,
            success,
            success ? null : message,
            CurrentOperator?.Id);

            // Log result
            if (success)
            {
                _messageConsole.AddSuccess($"Unit {serialNumber} downloaded successfully", "Download");

                // Set initial test to first required test (or test 1 if all required)
                SetInitialCurrentTest();

                // Persist unit data to SQLite for crash recovery (must complete before returning)
                await _stateStorage.UpdateUnitDataAsync(CurrentUnit);
            }
            else
            {
                _messageConsole.AddError($"Download failed: {message}", "Download");
            }

            // Notify UI that download completed (hide progress overlay)
            OnDownloadCompleted?.Invoke(success);

            return (success, message);
        }
        catch (Exception ex)
        {
            _messageConsole.AddError($"Download error: {ex.Message}", "Download");
            // Ensure overlay is hidden even on exception
            OnDownloadCompleted?.Invoke(false);
            throw;
        }
    }

    private static bool IsSerialValid(string? serial)
        => !string.IsNullOrWhiteSpace(serial) && serial.All(char.IsDigit) && serial.Length >= 3;

    // Public validation methods for UI components -------------------------------------
    public bool ValidateSerialNumber(string? serialNumber)
        => IsSerialValid(serialNumber);

    public (bool IsValid, string Message) ValidateDownload(string? serialNumber, string? catalogNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            return (false, "Please enter a serial number.");

        if (!IsSerialValid(serialNumber))
            return (false, "Serial must be numeric and at least 3 digits.");

        if (CurrentUnit != null && string.Equals(CurrentUnit.SerialNumber, serialNumber, StringComparison.Ordinal))
            return (true, "Ready to re-download (will refresh data).");

        if (CurrentUnit != null)
            return (true, "Ready to download (will replace current unit).");

        return (true, "Ready to download.");
    }

    // Test data access methods --------------------------------------------------------
    public List<HipotData>? GetHipotTestData()
        => CurrentUnit?.Hipot;

    public List<InducedData>? GetInducedTestData()
        => CurrentUnit?.Induced;

    public List<ImpulseData>? GetImpulseTestData()
        => CurrentUnit?.Impulse;

    public List<Ratings>? GetRatings()
        => CurrentUnit?.Ratings;

    public HipotData? GetCurrentHipotTest()
        => CurrentUnit?.Hipot.ElementAtOrDefault(CurrentUnit.CurrentTest - 1);

    public InducedData? GetCurrentInducedTest()
        => CurrentUnit?.Induced.ElementAtOrDefault(CurrentUnit.CurrentTest - 1);

    public ImpulseData? GetCurrentImpulseTest()
        => CurrentUnit?.Impulse.ElementAtOrDefault(CurrentUnit.CurrentTest - 1);


    // Initialization -----------------------------------------------------------------

    /// <summary>
    /// Initializes the test manager by loading cached options (BIL values, etc.) from storage.
    /// Also attempts to restore previous session state (operator and unit data, silently, no user prompt).
    /// Call this method once at application startup.
    /// </summary>
    public async Task InitializeAsync()
    {

        Console.WriteLine("Initializing DielectricTestManager...");
        // Load BIL options cache
        await _optionsStorageService.InitializeAsync();

        Console.WriteLine("Options cache loaded.");
        // Attempt to restore full session from previous state (operator + unit data)
        await RestoreSessionFromPersistenceAsync();
        Console.WriteLine("Session restoration attempt complete.");
    }

    /// <summary>
    /// Attempts to restore the complete session (operator + unit data) from the last saved session.
    /// Called automatically during InitializeAsync. Restores silently without prompting.
    /// If operator is found in saved session, they are automatically restored.
    /// If unit data is found, it is restored without showing the login dialog.
    /// </summary>
    private async Task RestoreSessionFromPersistenceAsync()
    {
        try
        {
            var lastSession = await _stateStorage.LoadLastSessionAsync();

            if (lastSession == null)
            {
                Console.WriteLine("No previous session to restore");
                return;
            }

            // Restore operator if present in saved session
            if (lastSession.Operator != null)
            {
                var (success, message) = await _employeeService.RestoreOperatorAsync(lastSession.Operator);
                if (success)
                {
                    _messageConsole.AddSuccess(
                    $"Operator session restored: {lastSession.Operator.Name}",
                    "Session");
                    Console.WriteLine($"Operator restored from session: {lastSession.Operator.Name}");
                }
                else
                {
                    _messageConsole.AddWarning($"Could not restore operator: {message}", "Session");
                    Console.WriteLine($"Failed to restore operator: {message}");
                }
            }

            // Restore unit data if present in saved session
            if (lastSession.UnitData != null)
            {
                // Restore unit data to UnitDataService
                _unitDataService.GetType()
                    .GetField("_currentUnit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?
                    .SetValue(_unitDataService, lastSession.UnitData);

                // Trigger UI update
                OnUnitDataChanged?.Invoke();

                _messageConsole.AddSuccess(
                $"Unit data restored from session: {lastSession.UnitData.SerialNumber}",
                "Session");

                Console.WriteLine($"Unit data restored from session: Serial={lastSession.UnitData.SerialNumber}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to restore session from persistence: {ex.Message}");
            _messageConsole.AddWarning("Could not restore previous session data", "Session");
        }
    }

    // UnitData options for Select components -----------------------------------------

    /// <summary>
    /// Gets cached primary BIL options (synchronous, loaded at startup).
    /// </summary>
    public IEnumerable<string> GetPrimaryBILOptions()
        => _optionsStorageService.PrimaryBILs;

    /// <summary>
    /// Gets cached secondary BIL options (synchronous, loaded at startup).
    /// </summary>
    public IEnumerable<string> GetSecondaryBILOptions()
        => _optionsStorageService.SecondaryBILs;
    private void SetCurrentTestActions(TestActions newTestAction)
    {
        CurrentTestActions = newTestAction;
        _messageConsole.AddInfo($"Navigating to {newTestAction}", "Navigation");
        OnTestActionChanged?.Invoke();
    }

    // Navigation methods for function keys -------------------------------------------

    /// <summary>
    /// Navigate to a specific test action (screen/context)
    /// </summary>
    /// <param name="action">The test action to navigate to</param>
    public void NavigateTo(TestActions action)
    {
        SetCurrentTestActions(action);
    }


    // Action methods for function keys -----------------------------------------------

    /// <summary>
    /// Show operator ID entry dialog
    /// </summary>
    public void ShowOperatorDialog()
    {
        _messageConsole.AddInfo("Showing operator login dialog (F7 pressed)", "Dialog");
        OnShowOperatorDialog?.Invoke();
    }

    /// <summary>
    /// Login an operator by employee number (facade to EmployeeService)
    /// </summary>
    public Task<(bool Success, string Message)> LoginOperatorAsync(string employeeNumber)
        => _employeeService.LoginAsync(employeeNumber);

    /// <summary>
    /// Logout the current operator (facade to EmployeeService)
    /// </summary>
    public void LogoutOperator()
        => _employeeService.Logout();

    /// <summary>
    /// Upload test results to server
    /// </summary>
    public void UploadResults()
    {
        _messageConsole.AddInstruction("UploadResults - not yet implemented", "Upload");
    }

    /// <summary>
    /// Reprint failure tag for current unit
    /// </summary>
    public void ReprintFailTag()
    {
        _messageConsole.AddInstruction("ReprintFailTag - not yet implemented", "Print");
    }

    /// <summary>
    /// Cycle to next test (wraps around to first test)
    /// </summary>
    public void CycleCurrentTest()
    {
        if (CurrentUnit == null || CurrentUnit.TotalTests <= 1)
            return;

        CurrentUnit.CurrentTest++;
        if (CurrentUnit.CurrentTest > CurrentUnit.TotalTests)
        {
            CurrentUnit.CurrentTest = 1;
        }

        OnUnitDataChanged?.Invoke();
    }

    /// <summary>
    /// Clear current unit data (with confirmation)
    /// </summary>
    public void ClearUnitData()
    {
        _messageConsole.AddInstruction("ClearUnitData - not yet implemented (needs confirmation)", "UnitData");
    }
}
