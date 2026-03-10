using MediatR;

namespace Auth.Application.Permissions.Commands.CreatePermission;

public sealed record CreatePermissionCommand(string Domain, string Code, string Description) : IRequest<PermissionDto>;
