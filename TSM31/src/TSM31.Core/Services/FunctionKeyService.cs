// Copyright (c) Microsoft.All rights reserved.
// Licensed under the MIT License.
namespace TSM31.Core.Services;

using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

public class FunctionKeyService : IAsyncDisposable
{
    private readonly DielectricTestManager _dielectricTestManager;
    private readonly IJSRuntime _jsRuntime;
    private readonly MessageConsoleService _messageConsoleService;
    private readonly ILogger<FunctionKeyService> _logger;

    // JS interop references
    private IJSObjectReference? _module;
    private DotNetObjectReference<FunctionKeyService>? _selfRef;

    // Events
    public event Action<string>? OnFunctionKeyPressed;

    public FunctionKeyService(
        DielectricTestManager dielectricTestManager,
        IJSRuntime jsRuntime,
        MessageConsoleService messageConsoleService,
        ILogger<FunctionKeyService> logger)
    {
        _dielectricTestManager = dielectricTestManager;
        _jsRuntime = jsRuntime;
        _messageConsoleService = messageConsoleService;
        _logger = logger;

        // Subscribe to TestAction changes to update function keys
        _dielectricTestManager.OnTestActionChanged += UpdateFunctionKeys;

        // Subscribe to state changes that affect key availability
        _dielectricTestManager.OnPendingSerialChanged += UpdateFunctionKeys;
        _dielectricTestManager.OnUnitDataChanged += UpdateFunctionKeys;

        // Subscribe to operator login/logout to update keys accordingly
        _dielectricTestManager.OnOperatorChanged += UpdateFunctionKeys;

        // Initialize keys based on current TestAction
        UpdateFunctionKeys();
    }

    /// <summary>
    /// Initialize JS interop for global keyboard handling
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Import globalKeys.js module
            try
            {
                _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "/_content/TSM31.Core/js/globalKeys.js");
            }
            catch
            {
                _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/globalKeys.js");
            }

            // Create DotNet reference for JS callbacks
            _selfRef = DotNetObjectReference.Create(this);

            // Register global keyboard handler
            await _module.InvokeVoidAsync("registerGlobalKeys", _selfRef);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize FunctionKeyService");
        }
    }

    /// <summary>
    /// Called by JavaScript when a function key is pressed
    /// </summary>
    [JSInvokable]
    public void OnGlobalKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        // Normalize key names
        var normalizedKey = NormalizeKeyText(key);

        // Invoke the key action
        InvokeKey(normalizedKey);
    }

    /// <summary>
    /// Normalize key text from JS event to our format
    /// </summary>
    private static string NormalizeKeyText(string key)
    {
        var upper = key.ToUpperInvariant();

        // Convert "ESCAPE" to "ESC"
        return upper == "ESCAPE" ? "ESC" : upper;

    }

    /// <summary>
    /// Execute a function key's action if it is enabled
    /// </summary>
    public void InvokeKey(string keyText)
    {
        var key = FunctionKeys.FirstOrDefault(k => k.KeyText.Equals(keyText, StringComparison.OrdinalIgnoreCase));

        if (key is not { IsEnabled: true }) return;
        try
        {
            key.OnKeyDown();
            OnFunctionKeyPressed?.Invoke(keyText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking function key {KeyText}", keyText);
        }
    }

    /// <summary>
    /// Update function key actions and labels based on current TestAction
    /// </summary>
    private void UpdateFunctionKeys()
    {
        switch (_dielectricTestManager.CurrentTestActions)
        {
            case TestActions.DataReview:
                ConfigureDataReviewKeys();
                break;

            case TestActions.DataEntry:
                ConfigureDataEntryKeys();
                break;

            case TestActions.FirstInduced:
            case TestActions.SecondInduced:
                ConfigureInducedKeys();
                break;

            case TestActions.Hipot:
                ConfigureHipotKeys();
                break;

            case TestActions.Impulse:
                ConfigureImpulseKeys();
                break;

            default:
                ConfigureDefaultKeys();
                break;
        }

        // Notify subscribers that keys have changed
        OnFunctionKeyPressed?.Invoke(string.Empty);
    }

    /// <summary>
    /// Configure keys for Data Review context
    /// </summary>
    private void ConfigureDataReviewKeys()
    {
        // if operator is not logged in, disable all keys except F7 (Operator ID)
        var operatorLoggedIn = _dielectricTestManager.IsOperatorLoggedIn;
        if (!operatorLoggedIn)
        {
            ConfigureDefaultKeys();
            UpdateKeyAction("F7", "Operator ID", () => _dielectricTestManager.ShowOperatorDialog());
            return;
        }

        UpdateKeyAction("ESC", "", () => {}, false);// Disabled
        UpdateKeyAction("F1", "Data Entry", () => _dielectricTestManager.NavigateTo(TestActions.DataEntry));
        UpdateKeyAction("F2", "First Induced", () => _dielectricTestManager.NavigateTo(TestActions.FirstInduced), false);// TODO: Check if required
        // enable if Hipot Test is required for this unit
        UpdateKeyAction("F3", "Hipot Test", () => _dielectricTestManager.NavigateTo(TestActions.Hipot), _dielectricTestManager.GetCurrentHipotTest() != null);// TODO: Check preconditions
        UpdateKeyAction("F4", "Impulse Test", () => _dielectricTestManager.NavigateTo(TestActions.Impulse), false);// TODO: Check preconditions
        UpdateKeyAction("F5", "Second Induced", () => _dielectricTestManager.NavigateTo(TestActions.SecondInduced), false);// TODO: Check preconditions
        UpdateKeyAction("F6", "Auto Test", () => {/* Auto test */
        }, false);// Disabled per docs
        UpdateKeyAction("F7", "Operator ID", () => _dielectricTestManager.ShowOperatorDialog());
        UpdateKeyAction("F8", "Upload", () => _dielectricTestManager.UploadResults());
        UpdateKeyAction("F9", "Reprint", () => _dielectricTestManager.ReprintFailTag());
        UpdateKeyAction("F10", "Next Test", () => _dielectricTestManager.CycleCurrentTest(), HasMultipleTests());
    }

    /// <summary>
    /// Configure keys for Data Entry context
    /// </summary>
    private void ConfigureDataEntryKeys()
    {
        var unitDownloaded = IsUnitDownloaded();

        _logger.LogDebug("ConfigureDataEntryKeys: unitDownloaded={UnitDownloaded}, pendingSerial={PendingSerial}",
            unitDownloaded, _dielectricTestManager.PendingSerialNumber);

        UpdateKeyAction("ESC", "Return", () => _dielectricTestManager.NavigateTo(TestActions.DataReview));

        // F1: Download - ALWAYS enabled in DataEntry mode
        // The handler in DataEntryTab will focus the input if no serial is present
        // or validate and download if a serial is entered
        UpdateKeyAction("F1", "Download", () => { _dielectricTestManager.DataEntryF1Handler?.Invoke(); });// Always enabled

        UpdateKeyAction("F2", "", () => {}, false);// Disabled
        UpdateKeyAction("F3", "", () => {}, false);// Disabled
        UpdateKeyAction("F4", "", () => {}, false);// Disabled
        UpdateKeyAction("F5", "", () => {}, false);// Disabled
        UpdateKeyAction("F6", "", () => {}, false);// Disabled
        UpdateKeyAction("F7", "", () => {}, false);// Disabled
        UpdateKeyAction("F8", "", () => {}, false);// Disabled

        // F9: Clear Unit - enabled only when unit is downloaded
        UpdateKeyAction("F9", "Clear Unit", () => {
            _dielectricTestManager.DataEntryF9Handler?.Invoke();
        }, unitDownloaded);

        UpdateKeyAction("F10", "Next Test", () => _dielectricTestManager.CycleCurrentTest(), HasMultipleTests());
    }

    /// <summary>
    /// Configure keys for Induced test context (First or Second)
    /// </summary>
    private void ConfigureInducedKeys()
    {
        UpdateKeyAction("ESC", "Return", () => _dielectricTestManager.NavigateTo(TestActions.DataReview));
        UpdateKeyAction("F1", "Test Power", () => {/* Toggle power */
        });// TODO: Implement
        UpdateKeyAction("F2", "Set & Record", () => {/* Set and record */
        });// TODO: Implement
        UpdateKeyAction("F3", "Start Timer", () => {/* Start timer */
        }, false);// TODO: Check preconditions
        UpdateKeyAction("F4", "Mode", () => {/* Toggle mode */
        });// TODO: Implement
        UpdateKeyAction("F5", "Fail Test", () => {/* Fail test */
        });// TODO: Implement
        UpdateKeyAction("F6", "", () => {}, false);// Disabled
        UpdateKeyAction("F7", "Set Condition", () => {/* Set condition */
        });// TODO: Implement
        UpdateKeyAction("F8", "Voltage Range", () => {/* Voltage range */
        });// TODO: Implement
        UpdateKeyAction("F9", "Current Range", () => {/* Current range */
        });// TODO: Implement
        UpdateKeyAction("F10", "Next Test", () => _dielectricTestManager.CycleCurrentTest(), HasMultipleTests());
    }

    /// <summary>
    /// Configure keys for Hipot test context
    /// </summary>
    private void ConfigureHipotKeys()
    {
        UpdateKeyAction("ESC", "Return", () => _dielectricTestManager.NavigateTo(TestActions.DataReview));
        UpdateKeyAction("F1", "Test Power", () => {/* Toggle power */
        });// TODO: Implement
        UpdateKeyAction("F2", "Set & Record", () => {/* Set and record */
        });// TODO: Implement
        UpdateKeyAction("F3", "Start Timer", () => {/* Start timer */
        }, false);// TODO: Check preconditions
        UpdateKeyAction("F4", "Mode", () => {/* Toggle mode */
        });// TODO: Implement
        UpdateKeyAction("F5", "Fail Test", () => {/* Fail test */
        });// TODO: Implement
        UpdateKeyAction("F6", "", () => {}, false);// Disabled
        UpdateKeyAction("F7", "Set Condition", () => {/* Set condition */
        });// TODO: Implement
        UpdateKeyAction("F8", "", () => {}, false);// Disabled
        UpdateKeyAction("F9", "Change Test", () => {/* Change test submenu */
        });// TODO: Implement
        UpdateKeyAction("F10", "Next Test", () => _dielectricTestManager.CycleCurrentTest(), HasMultipleTests());
    }

    /// <summary>
    /// Configure keys for Impulse test context
    /// </summary>
    private void ConfigureImpulseKeys()
    {
        UpdateKeyAction("ESC", "Return", () => _dielectricTestManager.NavigateTo(TestActions.DataReview));
        UpdateKeyAction("F1", "", () => {}, false);// Disabled
        UpdateKeyAction("F2", "Set & Trigger", () => {/* Set and trigger */
        });// TODO: Implement
        UpdateKeyAction("F3", "", () => {}, false);// Disabled
        UpdateKeyAction("F4", "", () => {}, false);// Disabled
        UpdateKeyAction("F5", "Fail Bushing", () => {/* Fail bushing */
        });// TODO: Implement
        UpdateKeyAction("F6", "", () => {}, false);// Disabled
        UpdateKeyAction("F7", "Set Condition", () => {/* Set condition */
        });// TODO: Implement
        UpdateKeyAction("F8", "", () => {}, false);// Disabled
        UpdateKeyAction("F9", "Change Bushing", () => {/* Change bushing submenu */
        });// TODO: Implement
        UpdateKeyAction("F10", "Next Test", () => _dielectricTestManager.CycleCurrentTest(), HasMultipleTests());
    }

    /// <summary>
    /// Configure default disabled state for all keys
    /// </summary>
    private void ConfigureDefaultKeys()
    {
        foreach (var key in FunctionKeys.ToList())
        {
            var index = FunctionKeys.IndexOf(key);
            FunctionKeys[index] = key with { IsEnabled = false };
        }
    }

    /// <summary>
    /// Registry of all function keys (ESC, F1-F12)
    /// </summary>
    /// 
    public List<FunctionKey> FunctionKeys { get; } = [
        new("ESC", "", () => {}, false),
        new("F1", "Data Entry", () => {}, false),
        new("F2", "First Induced", () => {}, false),
        new("F3", "Hipot Test", () => {}, false),
        new("F4", "Impulse Test", () => {}, false),
        new("F5", "Second Induced", () => {}, false),
        new("F6", "Auto Test", () => {}, false),
        new("F7", "Operator ID", () => {}, false),
        new("F8", "Upload", () => {}, false),
        new("F9", "Reprint", () => {}, false),
        new("F10", "Next Test", () => {}, false)
        // new FunctionKey("F11", "", () => { }, false),
        // new FunctionKey("F12", "", () => { }, false)
    ];

    /// <summary>
    /// Update a specific key's action, label, and enabled state
    /// </summary>
    public void UpdateKeyAction(string keyText, string label, Action newAction, bool isEnabled = true)
    {
        var key = FunctionKeys.FirstOrDefault(k => k.KeyText == keyText);
        if (key == null) return;
        var index = FunctionKeys.IndexOf(key);
        FunctionKeys[index] = new FunctionKey(keyText, label, newAction, isEnabled);
    }

    // Precondition helper methods

    private bool IsUnitDownloaded()
        => _dielectricTestManager.CurrentUnit != null;

    private bool HasMultipleTests()
        => _dielectricTestManager.CurrentUnit?.TotalTests > 1;

    private bool IsSerialNumberValid()
    {
        var serial = _dielectricTestManager.PendingSerialNumber;
        return !string.IsNullOrWhiteSpace(serial)
            && serial.Length == 10
            && serial.All(char.IsDigit);
    }

    public async ValueTask DisposeAsync()
    {
        // Unsubscribe from events
        _dielectricTestManager.OnTestActionChanged -= UpdateFunctionKeys;
        _dielectricTestManager.OnPendingSerialChanged -= UpdateFunctionKeys;
        _dielectricTestManager.OnUnitDataChanged -= UpdateFunctionKeys;
        _dielectricTestManager.OnOperatorChanged -= UpdateFunctionKeys;

        // Unregister global keyboard handler
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("unregisterGlobalKeys");
            }
        }
        catch
        {
            /* Ignore disposal errors */
        }

        // Dispose references
        _selfRef?.Dispose();
    }
}
