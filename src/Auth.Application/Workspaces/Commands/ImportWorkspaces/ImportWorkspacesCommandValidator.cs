using FluentValidation;

namespace Auth.Application.Workspaces.Commands.ImportWorkspaces;

public sealed class ImportWorkspacesCommandValidator : AbstractValidator<ImportWorkspacesCommand>
{
    public ImportWorkspacesCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            item.RuleFor(x => x.Code).NotEmpty().MaximumLength(200);
            item.RuleFor(x => x.Description).MaximumLength(500);
        });
    }
}
