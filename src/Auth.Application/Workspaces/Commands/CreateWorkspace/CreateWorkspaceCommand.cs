using MediatR;

namespace Auth.Application.Workspaces.Commands.CreateWorkspace;

public sealed record CreateWorkspaceCommand(string Name, string Code, string Description, bool IsSystem = false) : IRequest<WorkspaceDto>;
