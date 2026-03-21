using FluentValidation;

namespace Auth.Application.ServiceAccounts.Commands.UpdateServiceAccount;

public sealed class UpdateServiceAccountCommandValidator : AbstractValidator<UpdateServiceAccountCommand>
{
    public UpdateServiceAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
