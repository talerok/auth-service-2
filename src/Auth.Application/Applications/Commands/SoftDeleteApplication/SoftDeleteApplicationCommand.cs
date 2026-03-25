using Auth.Domain;
using MediatR;

namespace Auth.Application.Applications.Commands.SoftDeleteApplication;

public sealed record SoftDeleteApplicationCommand(Guid Id) : IRequest<bool>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.Application;
    public AuditAction Action => AuditAction.SoftDelete;
    public Guid EntityId => Id;
}
