using MediatR;

namespace Auth.Application.Permissions.Commands.PatchPermission;

public sealed record PatchPermissionCommand(Guid Id, string? Code, string? Description) : IRequest<PermissionDto?>;
