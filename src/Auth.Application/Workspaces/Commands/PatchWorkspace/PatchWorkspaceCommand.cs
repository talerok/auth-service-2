using MediatR;

namespace Auth.Application.Workspaces.Commands.PatchWorkspace;

public sealed record PatchWorkspaceCommand(Guid Id, string? Name, string? Code, string? Description) : IRequest<WorkspaceDto?>;
