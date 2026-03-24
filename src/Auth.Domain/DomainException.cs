namespace Auth.Domain;

public sealed class DomainException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
