using FluentValidation;

namespace Auth.Application.Workspaces.Commands.PatchWorkspace;

public sealed class PatchWorkspaceCommandValidator : AbstractValidator<PatchWorkspaceCommand>
{
    public PatchWorkspaceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
