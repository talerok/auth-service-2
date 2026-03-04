using MediatR;

namespace Auth.Application.Permissions.Commands.UpdatePermission;

public sealed record UpdatePermissionCommand(Guid Id, string Code, string Description) : IRequest<PermissionDto?>;
