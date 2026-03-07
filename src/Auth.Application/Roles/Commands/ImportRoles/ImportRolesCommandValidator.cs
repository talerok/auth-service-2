using FluentValidation;

namespace Auth.Application.Roles.Commands.ImportRoles;

public sealed class ImportRolesCommandValidator : AbstractValidator<ImportRolesCommand>
{
    public ImportRolesCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            item.RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
            item.RuleFor(x => x.Description).MaximumLength(500);
        });
    }
}
