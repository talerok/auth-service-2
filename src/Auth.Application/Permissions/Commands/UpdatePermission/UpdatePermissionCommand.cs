using MediatR;

namespace Auth.Application.Permissions.Commands.UpdatePermission;

public sealed record UpdatePermissionCommand(Guid Id, string Description) : IRequest<PermissionDto?>;
