using MediatR;

namespace Auth.Application.Permissions.Queries.ExportPermissions;

public sealed record ExportPermissionsQuery : IRequest<IReadOnlyCollection<ExportPermissionDto>>;
