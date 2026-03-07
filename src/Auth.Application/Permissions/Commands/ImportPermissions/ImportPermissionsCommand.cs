using MediatR;

namespace Auth.Application.Permissions.Commands.ImportPermissions;

public sealed record ImportPermissionsCommand(IReadOnlyCollection<ImportPermissionItem> Items, bool Add = true, bool Edit = true) : IRequest<ImportPermissionsResult>;
