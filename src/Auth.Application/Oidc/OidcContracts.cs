namespace Auth.Application;

public sealed record LoginRequest(string Username, string Password, string? ReturnUrl);

public sealed record MfaVerifyRequest(string? MfaToken, string? MfaChannel, string? Otp, string? ReturnUrl);

public sealed record ConsentRequest(
    string ClientId,
    IReadOnlyCollection<string> Scopes,
    bool Approved,
    string? ReturnUrl);
