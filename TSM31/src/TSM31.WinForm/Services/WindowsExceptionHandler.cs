namespace TSM31.WinForm.Services;

using Core.Resources;
using Core.Services.Contracts;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

public class WindowsExceptionHandler : ClientExceptionHandlerBase
{
    public WindowsExceptionHandler(ILogger<ClientExceptionHandlerBase> logger, IStringLocalizer<AppStrings> localizer)
        : base(logger, localizer)
    {
    }

    protected override void HandleInternal(Exception exception, ExceptionDisplayKind displayKind,
        Dictionary<string, object> parameters)
    {
        exception = UnWrapException(exception);

        if (IgnoreException(exception))
            return;

        base.HandleInternal(exception, displayKind, parameters);
    }
}
