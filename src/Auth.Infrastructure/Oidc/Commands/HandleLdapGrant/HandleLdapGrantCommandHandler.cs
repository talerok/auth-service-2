using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Application.Oidc.Commands.HandleLdapGrant;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using Auth.Application.Sessions.Commands.CreateSession;
using Auth.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Oidc.Commands.HandleLdapGrant;

internal sealed class HandleLdapGrantCommandHandler(
    AuthDbContext dbContext,
    ISender sender,
    ILdapAuthenticator ldapAuthenticator,
    IHttpContextAccessor httpContextAccessor,
    IOptions<IntegrationOptions> options) : IRequestHandler<HandleLdapGrantCommand, CredentialValidationResult>
{
    public async Task<CredentialValidationResult> Handle(HandleLdapGrantCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.IdentitySource)
            || string.IsNullOrWhiteSpace(command.Username)
            || string.IsNullOrWhiteSpace(command.Password))
            throw new AuthException(AuthErrorCatalog.InvalidRequest);

        var source = await dbContext.IdentitySources
            .Include(x => x.LdapConfig)
            .FirstOrDefaultAsync(x => x.Name == command.IdentitySource, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceNotFound);

        if (!source.IsEnabled)
            throw new AuthException(AuthErrorCatalog.IdentitySourceDisabled);

        if (source.Type != IdentitySourceType.Ldap || source.LdapConfig is null)
            throw new AuthException(AuthErrorCatalog.IdentitySourceTypeMismatch);

        if (source.LdapConfig.BindPassword is not null)
            source.LdapConfig.BindPassword = FieldEncryption.Decrypt(source.LdapConfig.BindPassword, options.Value.EncryptionKey);

        await ldapAuthenticator.AuthenticateAsync(source.LdapConfig, command.Username, command.Password, cancellationToken);

        var link = await dbContext.IdentitySourceLinks
            .FirstOrDefaultAsync(x => x.IdentitySourceId == source.Id && x.ExternalIdentity == command.Username, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceLinkNotFound);

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == link.UserId, cancellationToken)
            ?? throw new AuthException(AuthErrorCatalog.IdentitySourceLinkNotFound);

        if (!user.IsActive)
            throw new AuthException(AuthErrorCatalog.IdentitySourceUserInactive);

        if (user.TwoFactorEnabled)
        {
            var mfaChallenge = await sender.Send(
                new CreateLoginChallengeCommand(user.Id, user.TwoFactorChannel!.Value), cancellationToken);
            return new CredentialValidationResult.MfaRequired(mfaChallenge.Id, mfaChallenge.Channel);
        }

        var (ip, ua) = httpContextAccessor.GetClientInfo();
        var sessionId = await sender.Send(
            new CreateSessionCommand(user.Id, command.ClientId, "ldap", ip, ua), cancellationToken);
        var principal = await sender.Send(new BuildPrincipalQuery(user.Id, command.Scopes, command.ClientId, ["ldap"], sessionId), cancellationToken);
        return new CredentialValidationResult.Success(principal);
    }
}
