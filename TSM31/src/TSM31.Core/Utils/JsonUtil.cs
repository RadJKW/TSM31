namespace TSM31.Core.Utils;

using System.Text.Json;

public static class JsonUtil
{
    private readonly static JsonSerializerOptions JsOptions = new() { WriteIndented = true };

    public static string ToJsonPretty<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj, JsOptions);
    }
}

