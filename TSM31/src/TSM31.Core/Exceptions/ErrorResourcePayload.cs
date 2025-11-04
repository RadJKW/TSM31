namespace TSM31.Core.Exceptions;

public class ErrorResourcePayload
{
    public List<PropertyErrorResourceCollection> Details { get; set; } = [];
}

public class PropertyErrorResourceCollection
{
    public string? Name { get; set; } = "*";

    public List<ErrorResource> Errors { get; set; } = [];
}

public class ErrorResource
{
    public string? Key { get; set; }

    public string? Message { get; set; }
}
