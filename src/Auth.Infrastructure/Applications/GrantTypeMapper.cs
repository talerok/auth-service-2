using System.Globalization;
using Auth.Application;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using OidcPermissions = OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace Auth.Infrastructure.Applications;

internal static class GrantTypeMapper
{
    public static void ApplyGrantTypes(
        OpenIddictApplicationDescriptor descriptor,
        IReadOnlyCollection<string> grantTypes)
    {
        descriptor.Permissions.Add(OidcPermissions.Endpoints.Token);
        descriptor.Permissions.Add(OidcPermissions.Endpoints.Revocation);

        foreach (var gt in grantTypes)
        {
            switch (gt)
            {
                case "authorization_code":
                    descriptor.Permissions.Add(OidcPermissions.GrantTypes.AuthorizationCode);
                    descriptor.Permissions.Add(OidcPermissions.Endpoints.Authorization);
                    descriptor.Permissions.Add(OidcPermissions.Endpoints.EndSession);
                    descriptor.Permissions.Add(OidcPermissions.ResponseTypes.Code);
                    descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
                    break;

                case "client_credentials":
                    descriptor.Permissions.Add(OidcPermissions.GrantTypes.ClientCredentials);
                    break;

                case "refresh_token":
                    descriptor.Permissions.Add(OidcPermissions.GrantTypes.RefreshToken);
                    break;

                case "password":
                    descriptor.Permissions.Add(OidcPermissions.GrantTypes.Password);
                    break;

                case "jwt-bearer":
                    descriptor.Permissions.Add(OidcPermissions.Prefixes.GrantType + OidcConstants.JwtBearerGrantType);
                    break;

                case "ldap":
                    descriptor.Permissions.Add(OidcPermissions.Prefixes.GrantType + OidcConstants.LdapGrantType);
                    break;

                case "mfa_otp":
                    descriptor.Permissions.Add(OidcPermissions.Prefixes.GrantType + OidcConstants.MfaOtpGrantType);
                    break;
            }
        }
    }

    public static void ApplyTokenLifetimes(
        OpenIddictApplicationDescriptor descriptor,
        int? accessTokenLifetimeMinutes,
        int? refreshTokenLifetimeMinutes)
    {
        const string accessTokenKey = "oidc:token_lifetimes:access_token";
        const string refreshTokenKey = "oidc:token_lifetimes:refresh_token";

        if (accessTokenLifetimeMinutes.HasValue)
            descriptor.Settings[accessTokenKey] =
                TimeSpan.FromMinutes(accessTokenLifetimeMinutes.Value).ToString("c", CultureInfo.InvariantCulture);
        else
            descriptor.Settings.Remove(accessTokenKey);

        if (refreshTokenLifetimeMinutes.HasValue)
            descriptor.Settings[refreshTokenKey] =
                TimeSpan.FromMinutes(refreshTokenLifetimeMinutes.Value).ToString("c", CultureInfo.InvariantCulture);
        else
            descriptor.Settings.Remove(refreshTokenKey);
    }
}
