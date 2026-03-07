using FluentValidation;

namespace Auth.Application.IdentitySources.Commands.CreateIdentitySource;

public sealed class CreateIdentitySourceCommandValidator : AbstractValidator<CreateIdentitySourceCommand>
{
    public CreateIdentitySourceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}
