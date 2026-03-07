using Auth.Application;
using Auth.Application.IdentitySources.Commands.CreateIdentitySource;
using Auth.Domain;
using MediatR;

namespace Auth.Infrastructure.IdentitySources.Commands.CreateIdentitySource;

internal sealed class CreateIdentitySourceCommandHandler(
    AuthDbContext dbContext) : IRequestHandler<CreateIdentitySourceCommand, IdentitySourceDetailDto>
{
    public async Task<IdentitySourceDetailDto> Handle(CreateIdentitySourceCommand command, CancellationToken cancellationToken)
    {
        if (command.Type == IdentitySourceType.Oidc && command.OidcConfig is null)
            throw new AuthException(AuthErrorCatalog.IdentitySourceTypeMismatch);

        if (command.Type == IdentitySourceType.Ldap && command.LdapConfig is null)
            throw new AuthException(AuthErrorCatalog.IdentitySourceTypeMismatch);

        var source = new IdentitySource
        {
            Name = command.Name,
            Code = command.Code,
            DisplayName = command.DisplayName,
            Type = command.Type,
            IsEnabled = true
        };

        if (command.OidcConfig is not null)
        {
            source.OidcConfig = new IdentitySourceOidcConfig
            {
                IdentitySourceId = source.Id,
                Authority = command.OidcConfig.Authority,
                ClientId = command.OidcConfig.ClientId,
                ClientSecret = command.OidcConfig.ClientSecret
            };
        }

        if (command.LdapConfig is not null)
        {
            source.LdapConfig = new IdentitySourceLdapConfig
            {
                IdentitySourceId = source.Id,
                Host = command.LdapConfig.Host,
                Port = command.LdapConfig.Port,
                BaseDn = command.LdapConfig.BaseDn,
                BindDn = command.LdapConfig.BindDn,
                BindPassword = command.LdapConfig.BindPassword,
                UseSsl = command.LdapConfig.UseSsl,
                SearchFilter = command.LdapConfig.SearchFilter
            };
        }

        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync(cancellationToken);

        return IdentitySourceMapper.ToDetailDto(source);
    }
}
