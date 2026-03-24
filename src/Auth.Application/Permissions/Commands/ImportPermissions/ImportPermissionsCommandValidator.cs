using FluentValidation;

namespace Auth.Application.Permissions.Commands.ImportPermissions;

public sealed class ImportPermissionsCommandValidator : AbstractValidator<ImportPermissionsCommand>
{
    public ImportPermissionsCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Domain).NotEmpty().MaximumLength(120);
            item.RuleFor(x => x.Bit).GreaterThanOrEqualTo(0).LessThan(1024);
            item.RuleFor(x => x.Code).NotEmpty().MaximumLength(200);
            item.RuleFor(x => x.Description).MaximumLength(500);
        });
    }
}
