using MediatR;

namespace Auth.Application.Permissions.Queries.GetPermissionById;

public sealed record GetPermissionByIdQuery(Guid Id) : IRequest<PermissionDto?>;
