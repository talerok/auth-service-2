using Auth.Application;
using Auth.Application.IdentitySources.Commands.UpdateIdentitySource;
using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.IdentitySources.Commands.UpdateIdentitySource;

internal sealed class UpdateIdentitySourceCommandHandler(
    AuthDbContext dbContext,
    IOptions<IntegrationOptions> options,
    IAuditContext auditContext) : IRequestHandler<UpdateIdentitySourceCommand, IdentitySourceDetailDto>
{
    public async Task<IdentitySourceDetailDto> Handle(UpdateIdentitySourceCommand command, CancellationToken cancellationToken)
    {
        var encryptionKey = options.Value.EncryptionKey;

        var source = await dbContext.IdentitySources
            .Include(x => x.OidcConfig)
            .Include(x => x.LdapConfig)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        source.Code = command.Code;
        source.DisplayName = command.DisplayName;
        source.IsEnabled = command.IsEnabled;

        if (command.OidcConfig is not null)
            UpdateOidcConfig(source, command.OidcConfig, encryptionKey);

        if (command.LdapConfig is not null)
            UpdateLdapConfig(source, command.LdapConfig, encryptionKey);

        var changes = AuditDiff.CaptureChanges(dbContext.Entry(source));
        if (changes.Count > 0)
            auditContext.Details = changes;

        await dbContext.SaveChangesAsync(cancellationToken);

        return IdentitySourceMapper.ToDetailDto(source);
    }

    private static void UpdateOidcConfig(IdentitySource source, CreateOidcConfigRequest config, string encryptionKey)
    {
        if (source.OidcConfig is not null)
        {
            source.OidcConfig.Authority = config.Authority;
            source.OidcConfig.ClientId = config.ClientId;
            if (config.ClientSecret is not null)
                source.OidcConfig.ClientSecret = FieldEncryption.Encrypt(config.ClientSecret, encryptionKey);
        }
        else
        {
            source.OidcConfig = new IdentitySourceOidcConfig
            {
                IdentitySourceId = source.Id,
                Authority = config.Authority,
                ClientId = config.ClientId,
                ClientSecret = config.ClientSecret is not null
                    ? FieldEncryption.Encrypt(config.ClientSecret, encryptionKey)
                    : null
            };
        }
    }

    private static void UpdateLdapConfig(IdentitySource source, CreateLdapConfigRequest config, string encryptionKey)
    {
        if (source.LdapConfig is not null)
        {
            source.LdapConfig.Host = config.Host;
            source.LdapConfig.Port = config.Port;
            source.LdapConfig.BaseDn = config.BaseDn;
            source.LdapConfig.BindDn = config.BindDn;
            source.LdapConfig.UseSsl = config.UseSsl;
            source.LdapConfig.SearchFilter = config.SearchFilter;
            if (config.BindPassword is not null)
                source.LdapConfig.BindPassword = FieldEncryption.Encrypt(config.BindPassword, encryptionKey);
        }
        else
        {
            source.LdapConfig = new IdentitySourceLdapConfig
            {
                IdentitySourceId = source.Id,
                Host = config.Host,
                Port = config.Port,
                BaseDn = config.BaseDn,
                BindDn = config.BindDn,
                BindPassword = config.BindPassword is not null
                    ? FieldEncryption.Encrypt(config.BindPassword, encryptionKey)
                    : null,
                UseSsl = config.UseSsl,
                SearchFilter = config.SearchFilter
            };
        }
    }
}
