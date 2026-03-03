using MediatR;

namespace Auth.Application.Workspaces.Commands.SoftDeleteWorkspace;

public sealed record SoftDeleteWorkspaceCommand(Guid Id) : IRequest<bool>;
