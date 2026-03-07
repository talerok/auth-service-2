using MediatR;

namespace Auth.Application.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(string Name, string Code, string Description) : IRequest<RoleDto>;
