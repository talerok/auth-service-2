namespace Auth.Application.Applications;

internal static class UriValidation
{
    public static bool BeValidAbsoluteUri(string? uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out _);

    public static bool BeValidRedirectUri(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var parsed) &&
        (parsed.Scheme == "https" || (parsed.Scheme == "http" && parsed.Host == "localhost"));

    public static bool BeValidOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var parsed) &&
        parsed.Scheme is "http" or "https" &&
        parsed.PathAndQuery == "/" &&
        string.IsNullOrEmpty(parsed.Fragment);
}
