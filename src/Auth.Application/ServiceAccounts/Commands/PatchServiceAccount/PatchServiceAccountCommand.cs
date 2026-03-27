using Auth.Application.Common;
using Auth.Domain;
using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.PatchServiceAccount;

public sealed record PatchServiceAccountCommand(
    Guid Id,
    Optional<string> Name,
    Optional<string> Description,
    Optional<bool> IsActive,
    Optional<IReadOnlyCollection<string>> Audiences,
    Optional<int?> AccessTokenLifetimeMinutes) : IRequest<ServiceAccountDto?>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.ServiceAccount;
    public AuditAction Action => AuditAction.Patch;
    public Guid EntityId => Id;
}
