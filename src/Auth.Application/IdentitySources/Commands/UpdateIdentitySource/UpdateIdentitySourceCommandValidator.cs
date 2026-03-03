using FluentValidation;

namespace Auth.Application.IdentitySources.Commands.UpdateIdentitySource;

public sealed class UpdateIdentitySourceCommandValidator : AbstractValidator<UpdateIdentitySourceCommand>
{
    public UpdateIdentitySourceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}
