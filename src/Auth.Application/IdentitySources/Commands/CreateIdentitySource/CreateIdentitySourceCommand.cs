using Auth.Domain;
using MediatR;

namespace Auth.Application.IdentitySources.Commands.CreateIdentitySource;

public sealed record CreateIdentitySourceCommand(
    string Name,
    string Code,
    string DisplayName,
    IdentitySourceType Type,
    CreateOidcConfigRequest? OidcConfig = null,
    CreateLdapConfigRequest? LdapConfig = null) : IRequest<IdentitySourceDetailDto>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.IdentitySource;
    public AuditAction Action => AuditAction.Create;
    public Guid EntityId { get; init; } = Guid.NewGuid();
}
