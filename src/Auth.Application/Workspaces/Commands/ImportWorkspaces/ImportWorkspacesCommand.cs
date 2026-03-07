using MediatR;

namespace Auth.Application.Workspaces.Commands.ImportWorkspaces;

public sealed record ImportWorkspacesCommand(IReadOnlyCollection<ImportWorkspaceItem> Items, bool Add = true, bool Edit = true) : IRequest<ImportWorkspacesResult>;
