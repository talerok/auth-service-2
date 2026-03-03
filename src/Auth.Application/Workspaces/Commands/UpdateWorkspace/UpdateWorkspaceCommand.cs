using MediatR;

namespace Auth.Application.Workspaces.Commands.UpdateWorkspace;

public sealed record UpdateWorkspaceCommand(Guid Id, string Name, string Code, string Description) : IRequest<WorkspaceDto?>;
