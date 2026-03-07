using MediatR;

namespace Auth.Application.IdentitySources.Commands.UpdateIdentitySource;

public sealed record UpdateIdentitySourceCommand(
    Guid Id,
    string Code,
    string DisplayName,
    bool IsEnabled,
    CreateOidcConfigRequest? OidcConfig = null,
    CreateLdapConfigRequest? LdapConfig = null) : IRequest<IdentitySourceDetailDto>;
