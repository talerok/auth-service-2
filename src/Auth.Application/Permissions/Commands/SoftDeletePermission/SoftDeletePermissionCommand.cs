using MediatR;

namespace Auth.Application.Permissions.Commands.SoftDeletePermission;

public sealed record SoftDeletePermissionCommand(Guid Id) : IRequest<bool>;
