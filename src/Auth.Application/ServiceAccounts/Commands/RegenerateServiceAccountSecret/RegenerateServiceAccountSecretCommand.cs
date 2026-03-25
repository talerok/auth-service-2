using Auth.Domain;
using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.RegenerateServiceAccountSecret;

public sealed record RegenerateServiceAccountSecretCommand(Guid Id) : IRequest<RegenerateServiceAccountSecretResponse?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.ServiceAccount;
    public AuditAction Action => AuditAction.RegenerateSecret;
    public Guid EntityId => Id;
}
