using Auth.Application;
using Auth.Domain;

namespace Auth.Infrastructure.IdentitySources;

internal static class IdentitySourceMapper
{
    public static IdentitySourceDetailDto ToDetailDto(IdentitySource source) =>
        new(source.Id, source.Name, source.Code, source.DisplayName, source.Type, source.IsEnabled, source.CreatedAt,
            source.OidcConfig is not null
                ? new IdentitySourceOidcConfigDto(source.OidcConfig.Authority, source.OidcConfig.ClientId, source.OidcConfig.ClientSecret is not null)
                : null,
            source.LdapConfig is not null
                ? new IdentitySourceLdapConfigDto(source.LdapConfig.Host, source.LdapConfig.Port, source.LdapConfig.BaseDn, source.LdapConfig.UseSsl)
                : null);
}
