using FluentValidation;

namespace Auth.Application.Workspaces.Commands.PatchWorkspace;

public sealed class PatchWorkspaceCommandValidator : AbstractValidator<PatchWorkspaceCommand>
{
    public PatchWorkspaceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name.Value!).NotNull().MaximumLength(200)
            .OverridePropertyName("Name").When(x => x.Name.HasValue);
        RuleFor(x => x.Code.Value!).NotNull().MaximumLength(200)
            .OverridePropertyName("Code").When(x => x.Code.HasValue);
        RuleFor(x => x.Description.Value!).NotNull().MaximumLength(500)
            .OverridePropertyName("Description").When(x => x.Description.HasValue);
    }
}
