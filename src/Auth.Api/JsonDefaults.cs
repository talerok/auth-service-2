using System.Text.Json;

namespace Auth.Api;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions IndentedCamelCase = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
