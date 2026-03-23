namespace Auth.Application;

public static class OidcConstants
{
    public const string MfaOtpGrantType = "urn:custom:mfa_otp";
    public const string JwtBearerGrantType = "urn:ietf:params:oauth:grant-type:jwt-bearer";
    public const string LdapGrantType = "urn:custom:ldap";

    public static readonly HashSet<string> AllowedGrantTypes = new(StringComparer.Ordinal)
    {
        "authorization_code",
        "client_credentials",
        "refresh_token",
        "password",
        "jwt-bearer",
        "ldap",
        "mfa_otp"
    };
}
