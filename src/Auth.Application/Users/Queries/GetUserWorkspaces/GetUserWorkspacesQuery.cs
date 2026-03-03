using MediatR;

namespace Auth.Application.Users.Queries.GetUserWorkspaces;

public sealed record GetUserWorkspacesQuery(Guid UserId) : IRequest<IReadOnlyCollection<UserWorkspaceRolesItem>?>;
