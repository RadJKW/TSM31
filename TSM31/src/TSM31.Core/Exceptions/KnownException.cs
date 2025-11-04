namespace TSM31.Core.Exceptions;

using Microsoft.Extensions.Localization;

public abstract class KnownException : Exception
{
    public KnownException(string message)
        : base(message)
    {
        Key = message;
    }

    public KnownException(string message, Exception? innerException)
        : base(message, innerException)
    {
        Key = message;
    }

    public KnownException(LocalizedString message)
        : base(message.Value)
    {
        Key = message.Name;
    }

    public KnownException(LocalizedString message, Exception? innerException)
        : base(message.Value, innerException)
    {
        Key = message.Name;
    }

    public string? Key { get; set; }

    /// <summary>
    /// Read KnownExceptionExtensions.WithExtensionData comments.
    /// </summary>
    public bool TryGetExtensionDataValue<T>(string key, out T value)
    {
        value = default!;

        if (Data[key] is not { } valueObj) return false;
        if (valueObj is T obj)
        {
            value = obj;
        }
        return true;

    }
}
