

// Added for NativeDialog

namespace TSM31.Core.Services.Contracts;

using Config;
using Exceptions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Resources;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public abstract class ClientExceptionHandlerBase : SharedExceptionHandler, IExceptionHandler
{
    private ILogger<ClientExceptionHandlerBase> Logger { get; }

    protected ClientExceptionHandlerBase(ILogger<ClientExceptionHandlerBase> logger,
        IStringLocalizer<AppStrings> localizer)
    {
        Logger = logger;
        Localizer = localizer;
    }

    public void Handle(Exception exception,
        ExceptionDisplayKind displayKind = ExceptionDisplayKind.Default,
        Dictionary<string, object?>? parameters = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        parameters = parameters ?? new Dictionary<string, object?>();

        parameters[nameof(filePath)] = filePath;
        parameters[nameof(memberName)] = memberName;
        parameters[nameof(lineNumber)] = lineNumber;
        parameters["exceptionId"] =
            Guid.NewGuid(); // This will remain consistent across different registered loggers, such as Sentry, Application Insights, etc.

        foreach (var item in GetExceptionData(exception))
        {
            parameters[item.Key] = item.Value;
        }

        HandleInternal(exception, displayKind, parameters.ToDictionary(i => i.Key, i => i.Value ?? string.Empty));
    }

    protected virtual void HandleInternal(Exception exception,
        ExceptionDisplayKind displayKind,
        Dictionary<string, object> parameters)
    {
        var isDevEnv = AppEnvironment.IsDevelopment();

        using (Logger.BeginScope(parameters.ToDictionary(i => i.Key, i => i.Value)))
        {
            var exceptionMessageToLog = GetExceptionMessageToLog(exception);

            if (exception is KnownException)
            {
                Logger.LogError(exception, exceptionMessageToLog);
            }
            else
            {
                Logger.LogCritical(exception, exceptionMessageToLog);
            }
        }

        var exceptionMessageToShow = GetExceptionMessageToShow(exception);

        if (displayKind is ExceptionDisplayKind.Default)
        {
            displayKind = GetDisplayKind(exception);
        }

        switch (displayKind)
        {
            case ExceptionDisplayKind.NonInterrupting:
                // Replaced SnackBar with native dialog (non-blocking intent retained but MessageBox is modal on Windows)
                NativeDialog.ShowError("Application Error", exceptionMessageToShow);
                break;
            case ExceptionDisplayKind.Interrupting:
                // Display a native Windows (or platform fallback) message box
                NativeDialog.ShowError("Critical Application Error", exceptionMessageToShow);
                break;
            case ExceptionDisplayKind.None when isDevEnv:
                Debugger.Break();
                break;
            case ExceptionDisplayKind.Default:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(displayKind), displayKind, null);
        }
    }

    // ReSharper disable once UnusedParameter.Local
    private static ExceptionDisplayKind GetDisplayKind(Exception exception)
    {
        // TODO: Implement other exception types as non-interrupting if needed

        return ExceptionDisplayKind.Interrupting;
    }

    public override bool IgnoreException(Exception _)
    {
        switch (_)
        {
            case TaskCanceledException:
            case OperationCanceledException:
            case TimeoutException:
            case JSException jsException when
                jsException.Message.Contains("JS object instance with ID", StringComparison.OrdinalIgnoreCase):
                return true;
            default:
                return base.IgnoreException(_);
        }
    }
}
