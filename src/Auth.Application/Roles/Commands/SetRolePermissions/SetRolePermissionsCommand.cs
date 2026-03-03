using MediatR;

namespace Auth.Application.Roles.Commands.SetRolePermissions;

public sealed record SetRolePermissionsCommand(Guid RoleId, IReadOnlyCollection<PermissionDto> Permissions) : IRequest;
