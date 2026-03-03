using Auth.Domain;
using MediatR;

namespace Auth.Application.IdentitySources.Commands.CreateIdentitySource;

public sealed record CreateIdentitySourceCommand(
    string Name,
    string DisplayName,
    IdentitySourceType Type,
    CreateOidcConfigRequest? OidcConfig = null,
    CreateLdapConfigRequest? LdapConfig = null) : IRequest<IdentitySourceDetailDto>;
