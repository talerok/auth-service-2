using MediatR;

namespace Auth.Application.Workspaces.Queries.GetWorkspaceById;

public sealed record GetWorkspaceByIdQuery(Guid Id) : IRequest<WorkspaceDto?>;
