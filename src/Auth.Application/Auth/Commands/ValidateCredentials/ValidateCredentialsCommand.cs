using Auth.Domain;
using MediatR;

namespace Auth.Application.Auth.Commands.ValidateCredentials;

public sealed record ValidateCredentialsCommand(string Username, string Password) : IRequest<User>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.Login;
    public Guid EntityId { get; init; }
    public bool Critical => true;
}
