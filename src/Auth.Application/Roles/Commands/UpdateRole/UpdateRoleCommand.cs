using MediatR;

namespace Auth.Application.Roles.Commands.UpdateRole;

public sealed record UpdateRoleCommand(Guid Id, string Name, string Code, string Description) : IRequest<RoleDto?>;
