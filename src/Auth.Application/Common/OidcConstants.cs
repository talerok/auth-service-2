namespace Auth.Application;

public static class OidcConstants
{
    public const string MfaOtpGrantType = "urn:custom:mfa_otp";
    public const string TokenExchangeGrantType = "urn:custom:token_exchange";

    public static readonly HashSet<string> AllowedGrantTypes = new(StringComparer.Ordinal)
    {
        "authorization_code",
        "client_credentials",
        "refresh_token",
        "password",
        "token_exchange",
        "mfa_otp"
    };
}
