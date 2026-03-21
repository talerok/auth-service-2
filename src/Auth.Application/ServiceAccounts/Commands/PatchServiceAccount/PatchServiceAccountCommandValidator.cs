using FluentValidation;

namespace Auth.Application.ServiceAccounts.Commands.PatchServiceAccount;

public sealed class PatchServiceAccountCommandValidator : AbstractValidator<PatchServiceAccountCommand>
{
    public PatchServiceAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
