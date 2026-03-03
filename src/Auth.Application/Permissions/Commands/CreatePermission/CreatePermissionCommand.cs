using MediatR;

namespace Auth.Application.Permissions.Commands.CreatePermission;

public sealed record CreatePermissionCommand(string Code, string Description) : IRequest<PermissionDto>;
