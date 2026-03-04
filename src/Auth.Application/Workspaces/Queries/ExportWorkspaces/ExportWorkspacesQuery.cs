using MediatR;

namespace Auth.Application.Workspaces.Queries.ExportWorkspaces;

public sealed record ExportWorkspacesQuery : IRequest<IReadOnlyCollection<ExportWorkspaceDto>>;
