using MediatR;

namespace Auth.Application.Roles.Commands.PatchRole;

public sealed record PatchRoleCommand(Guid Id, string? Name, string? Description) : IRequest<RoleDto?>;
