namespace TSM31.Core.Services.Contracts;

using Config;
using Exceptions;
using Microsoft.Extensions.Localization;
using Resources;
using System.Reflection;

public class SharedExceptionHandler
{
    protected IStringLocalizer<AppStrings> Localizer { get; set; } = default!;

    protected string GetExceptionMessageToShow(Exception exception)
    {
        if (exception is KnownException)
            return exception.Message;

        if (AppEnvironment.IsDevelopment())
            return exception.ToString();

        return Localizer[nameof(AppStrings.UnknownException)];
    }

    protected string GetExceptionMessageToLog(Exception exception)
    {
        var exceptionMessageToLog = exception.Message;
        var innerException = exception.InnerException;

        while (innerException is not null)
        {
            exceptionMessageToLog += $"{Environment.NewLine}{innerException.Message}";
            innerException = innerException.InnerException;
        }

        return exceptionMessageToLog;
    }

    public Exception UnWrapException(Exception exception)
    {
        if (exception is AggregateException aggregateException)
        {
            return aggregateException.Flatten().InnerException ?? aggregateException;
        }

        if (exception is TargetInvocationException)
        {
            return exception.InnerException ?? exception;
        }

        return exception;
    }

    public virtual bool IgnoreException(Exception exception)
    {
        // Ignoring exception here will prevent it from being logged in both client and server.

        /* --- Not Implemented --- */
        // if (exception is ClientNotSupportedException)
        //     return true; // See ExceptionDelegatingHandler

        if (exception is KnownException)
            return false;

        return exception.InnerException is not null && IgnoreException(exception.InnerException);
    }

    protected IDictionary<string, object?> GetExceptionData(Exception exp)
    {
        var data = exp.Data.Keys.Cast<string>()
            .Zip(exp.Data.Values.Cast<object?>())
            .ToDictionary(item => item.First, item => item.Second);

        // --- Not Implemented --- 09/30/2025
        // if (exp is ResourceValidationException resValExp)
        // {
        //     foreach (var detail in resValExp.Payload.Details)
        //     {
        //         foreach (var error in detail.Errors)
        //         {
        //             data[$"{detail.Name}:{error.Key}"] = error.Message;
        //         }
        //     }
        // }

        if (exp.InnerException is not null)
        {
            var innerData = GetExceptionData(exp.InnerException);

            foreach (var innerDataItem in innerData)
            {
                data[innerDataItem.Key] = innerDataItem.Value;
            }
        }

        return data;
    }
}
