using FluentValidation;

namespace Auth.Application.ServiceAccounts.Commands.PatchServiceAccount;

public sealed class PatchServiceAccountCommandValidator : AbstractValidator<PatchServiceAccountCommand>
{
    public PatchServiceAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name.Value!).NotNull().MaximumLength(200)
            .OverridePropertyName("Name").When(x => x.Name.HasValue);
        RuleFor(x => x.Description.Value!).NotNull().MaximumLength(500)
            .OverridePropertyName("Description").When(x => x.Description.HasValue);
        RuleFor(x => x.Audiences.Value).NotNull()
            .OverridePropertyName("Audiences").When(x => x.Audiences.HasValue);
    }
}
