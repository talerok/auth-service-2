using Auth.Domain;
using MediatR;

namespace Auth.Application.Users.Commands.ImportUsers;

public sealed record ImportUsersCommand(
    IReadOnlyCollection<ImportUserItem> Items,
    bool Add = true,
    bool Edit = true,
    bool BlockMissing = false) : IRequest<ImportUsersResult>, IAuditable
{
    public AuditEntityType EntityType => AuditEntityType.User;
    public AuditAction Action => AuditAction.Import;
    public Guid EntityId { get; init; } = Guid.NewGuid();
}
