using FluentValidation;

namespace Auth.Application.ServiceAccounts.Commands.CreateServiceAccount;

public sealed class CreateServiceAccountCommandValidator : AbstractValidator<CreateServiceAccountCommand>
{
    public CreateServiceAccountCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
