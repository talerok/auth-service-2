using MediatR;

namespace Auth.Application.Roles.Queries.GetRoleById;

public sealed record GetRoleByIdQuery(Guid Id) : IRequest<RoleDto?>;
