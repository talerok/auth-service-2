namespace Auth.Domain;

public class DomainException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
