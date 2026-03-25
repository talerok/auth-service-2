using Auth.Domain;
using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.UpdateServiceAccount;

public sealed record UpdateServiceAccountCommand(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    IReadOnlyCollection<string>? Audiences = null,
    int? AccessTokenLifetimeMinutes = null) : IRequest<ServiceAccountDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.ServiceAccount;
    public AuditAction Action => AuditAction.Update;
    public Guid EntityId => Id;
}
