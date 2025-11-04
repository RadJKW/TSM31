using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TSM31.Dielectric.Testing;

namespace TSM31.Dielectric.Navigation;

public class FunctionKeyService : IAsyncDisposable
{
    private readonly ITestManager _testManager;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<FunctionKeyService> _logger;
    private IJSObjectReference? _module;
    private DotNetObjectReference<FunctionKeyService>? _selfRef;

    public event Action<string>? OnFunctionKeyPressed;

    public List<FunctionKey> FunctionKeys { get; } = [
        new("ESC", "", () => {}, false),
        new("F1", "", () => {}, false),
        new("F2", "", () => {}, false),
        new("F3", "", () => {}, false),
        new("F4", "", () => {}, false),
        new("F5", "", () => {}, false),
        new("F6", "", () => {}, false),
        new("F7", "", () => {}, false),
        new("F8", "", () => {}, false),
        new("F9", "", () => {}, false),
        new("F10", "", () => {}, false)
    ];

    public FunctionKeyService(ITestManager testManager, IJSRuntime jsRuntime, ILogger<FunctionKeyService> logger)
    {
        _testManager = testManager;
        _jsRuntime = jsRuntime;
        _logger = logger;
        _logger.LogInformation("FunctionKeyService constructor - subscribing to events");
        _testManager.OnTestActionChanged += UpdateFunctionKeys;
        // _logger.LogInformation("FunctionKeyService constructor - performing initial UpdateFunctionKeys");
        UpdateFunctionKeys();
        // _logger.LogInformation("FunctionKeyService constructor complete. Keys: {@Keys}", FunctionKeys.Select(k => new { k.Key, k.Label, k.IsEnabled }));
    }

    public async Task InitializeAsync()
    {
        try
        {
            // _logger.LogInformation("InitializeAsync starting");
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/TSM31.Dielectric/js/globalKeys.js");
            _selfRef = DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("registerGlobalKeys", _selfRef);
            // _logger.LogInformation("InitializeAsync complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing FunctionKeyService");
        }
    }

    private void UpdateFunctionKeys()
    {
        var currentAction = _testManager.CurrentTestActions;
        // _logger.LogInformation("UpdateFunctionKeys called with CurrentTestActions: {CurrentAction}", currentAction);

        foreach (var key in FunctionKeys)
        {
            key.Label = "";
            key.Action = () => {};
            key.IsEnabled = false;
        }

        switch (currentAction)
        {
            case TestActions.Home:
                UpdateKeyAction("F1", "Data Entry", () => _testManager.NavigateTo(TestActions.DataEntry));
                UpdateKeyAction("F2", "Testing", () => _testManager.NavigateTo(TestActions.Testing));
                UpdateKeyAction("F7", "Operator ID", () => _testManager.ShowOperatorDialog());
                break;
            case TestActions.DataEntry:
                UpdateKeyAction("ESC", "Home", () => _testManager.NavigateTo(TestActions.Home));
                UpdateKeyAction("F1", "Start Test", () => _testManager.NavigateTo(TestActions.Testing));
                UpdateKeyAction("F7", "Operator ID", () => _testManager.ShowOperatorDialog());
                break;
            case TestActions.DataReview:
                UpdateKeyAction("ESC", "Home", () => _testManager.NavigateTo(TestActions.Home));
                UpdateKeyAction("F1", "Data Entry", () => _testManager.NavigateTo(TestActions.DataEntry));
                UpdateKeyAction("F7", "Operator ID", () => _testManager.ShowOperatorDialog());
                break;
            case TestActions.Testing:
                UpdateKeyAction("ESC", "Home", () => _testManager.NavigateTo(TestActions.Home));
                UpdateKeyAction("F7", "Operator ID", () => _testManager.ShowOperatorDialog());
                break;
        }

        // _logger.LogInformation("UpdateFunctionKeys complete. Active keys: {ActiveKeys}",
        // string.Join(", ", FunctionKeys.Where(k => k.IsEnabled).Select(k => $"{k.Key}={k.Label}")));

        OnFunctionKeyPressed?.Invoke("update");
    }

    private void UpdateKeyAction(string keyName, string label, Action action)
    {
        var key = FunctionKeys.FirstOrDefault(k => k.Key == keyName);
        if (key != null)
        {
            key.Label = label;
            key.Action = action;
            key.IsEnabled = true;
            _logger.LogDebug("Updated key {KeyName}: {Label}", keyName, label);
        }
    }

    [JSInvokable]
    public void HandleKeyPress(string key)
    {
        // _logger.LogInformation("HandleKeyPress called with key: {Key}", key);
        var functionKey = FunctionKeys.FirstOrDefault(k => k.Key == key);
        if (functionKey is { IsEnabled: true })
        {
            // _logger.LogInformation("Executing action for key: {Key} ({Label})", key, functionKey.Label);
            functionKey.Action();
            OnFunctionKeyPressed?.Invoke(key);
        }
        else
        {
            _logger.LogWarning("Key {Key} is disabled or not found", key);
        }
    }

    [JSInvokable]
    public void OnGlobalKey(string key)
    {
        // Normalize Escape key
        var normalizedKey = key == "Escape" ? "ESC" : key;
        HandleKeyPress(normalizedKey);
    }

    /// <summary>
    /// Invokes a function key action programmatically (e.g., from button click)
    /// </summary>
    public void InvokeKey(string key)
    {
        HandleKeyPress(key);
    }

    public async ValueTask DisposeAsync()
    {
        // _logger.LogInformation("FunctionKeyService disposing");
        if (_module != null)
        {
            try
            {
                await _module.InvokeVoidAsync("unregisterGlobalKeys");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error unregistering global keys");
            }
            await _module.DisposeAsync();
        }
        _selfRef?.Dispose();
        _testManager.OnTestActionChanged -= UpdateFunctionKeys;
    }
}
