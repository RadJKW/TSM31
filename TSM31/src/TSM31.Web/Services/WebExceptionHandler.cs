namespace TSM31.Web.Services;

using Core.Resources;
using Core.Services.Contracts;
using Microsoft.Extensions.Localization;

/// <summary>
/// Web-specific exception handler for ASP.NET Core applications
/// </summary>
public class WebExceptionHandler : ClientExceptionHandlerBase
{
    public WebExceptionHandler(
        ILogger<ClientExceptionHandlerBase> logger,
        IStringLocalizer<AppStrings> localizer)
        : base(logger, localizer)
    {
    }
}
