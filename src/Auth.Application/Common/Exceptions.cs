namespace Auth.Application;

public sealed class AuthException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
