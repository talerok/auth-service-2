using MediatR;

namespace Auth.Application.Permissions.Commands.ImportPermissions;

public sealed record ImportPermissionsCommand(IReadOnlyCollection<ImportPermissionItem> Items) : IRequest<ImportPermissionsResult>;
