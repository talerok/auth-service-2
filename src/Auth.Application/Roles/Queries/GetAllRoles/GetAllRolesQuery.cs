using MediatR;

namespace Auth.Application.Roles.Queries.GetAllRoles;

public sealed record GetAllRolesQuery() : IRequest<IReadOnlyCollection<RoleDto>>;
