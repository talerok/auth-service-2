using Auth.Application;
using Auth.Application.IdentitySources.Commands.UpdateIdentitySource;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.IdentitySources.Commands.UpdateIdentitySource;

internal sealed class UpdateIdentitySourceCommandHandler(
    AuthDbContext dbContext) : IRequestHandler<UpdateIdentitySourceCommand, IdentitySourceDetailDto>
{
    public async Task<IdentitySourceDetailDto> Handle(UpdateIdentitySourceCommand command, CancellationToken cancellationToken)
    {
        var source = await dbContext.IdentitySources
            .Include(x => x.OidcConfig)
            .Include(x => x.LdapConfig)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        source.Code = command.Code;
        source.DisplayName = command.DisplayName;
        source.IsEnabled = command.IsEnabled;
        source.UpdatedAt = DateTime.UtcNow;

        if (command.OidcConfig is not null)
        {
            if (source.OidcConfig is not null)
            {
                source.OidcConfig.Authority = command.OidcConfig.Authority;
                source.OidcConfig.ClientId = command.OidcConfig.ClientId;
                if (command.OidcConfig.ClientSecret is not null)
                    source.OidcConfig.ClientSecret = command.OidcConfig.ClientSecret;
            }
            else
            {
                source.OidcConfig = new IdentitySourceOidcConfig
                {
                    IdentitySourceId = source.Id,
                    Authority = command.OidcConfig.Authority,
                    ClientId = command.OidcConfig.ClientId,
                    ClientSecret = command.OidcConfig.ClientSecret
                };
            }
        }

        if (command.LdapConfig is not null)
        {
            if (source.LdapConfig is not null)
            {
                source.LdapConfig.Host = command.LdapConfig.Host;
                source.LdapConfig.Port = command.LdapConfig.Port;
                source.LdapConfig.BaseDn = command.LdapConfig.BaseDn;
                source.LdapConfig.BindDn = command.LdapConfig.BindDn;
                source.LdapConfig.UseSsl = command.LdapConfig.UseSsl;
                source.LdapConfig.SearchFilter = command.LdapConfig.SearchFilter;
                if (command.LdapConfig.BindPassword is not null)
                    source.LdapConfig.BindPassword = command.LdapConfig.BindPassword;
            }
            else
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
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return IdentitySourceMapper.ToDetailDto(source);
    }
}
