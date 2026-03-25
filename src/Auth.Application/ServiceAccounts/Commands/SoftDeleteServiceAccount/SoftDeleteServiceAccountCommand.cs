using Auth.Domain;
using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.SoftDeleteServiceAccount;

public sealed record SoftDeleteServiceAccountCommand(Guid Id) : IRequest<bool>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.ServiceAccount;
    public AuditAction Action => AuditAction.SoftDelete;
    public Guid EntityId => Id;
}
