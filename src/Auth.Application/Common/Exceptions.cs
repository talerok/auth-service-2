using Auth.Domain;

namespace Auth.Application;

public sealed class AuthException(string code) : DomainException(code);
