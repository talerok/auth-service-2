using MediatR;

namespace Auth.Application.Workspaces.Commands.ImportWorkspaces;

public sealed record ImportWorkspacesCommand(IReadOnlyCollection<ImportWorkspaceItem> Items) : IRequest<ImportWorkspacesResult>;
