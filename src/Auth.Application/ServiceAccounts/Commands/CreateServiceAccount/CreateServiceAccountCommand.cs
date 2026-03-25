using Auth.Domain;
using MediatR;

namespace Auth.Application.ServiceAccounts.Commands.CreateServiceAccount;

public sealed record CreateServiceAccountCommand(
    string Name,
    string Description,
    bool IsActive = true,
    IReadOnlyCollection<string>? Audiences = null,
    int? AccessTokenLifetimeMinutes = null) : IRequest<CreateServiceAccountResponse>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.ServiceAccount;
    public AuditAction Action => AuditAction.Create;
    public Guid EntityId { get; init; } = Guid.NewGuid();
}
