namespace PluginRuntime.Api.Helpers;

public static class ErrorCategoryMapper
{
    public static int GetHttpStatus(string category) => category switch
    {
        "Validation" => 400,
        "Security" => 403,
        "NotFound" => 404,
        "Execution" => 500,
        "Timeout" => 504,
        "ResourceLimit" => 429,
        _ => 500
    };
}
