using MediatR;

namespace Auth.Application.Roles.Queries.ExportRoles;

public sealed record ExportRolesQuery : IRequest<IReadOnlyCollection<ExportRoleDto>>;
