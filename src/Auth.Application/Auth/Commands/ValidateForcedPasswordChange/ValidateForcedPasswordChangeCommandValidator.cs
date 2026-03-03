using FluentValidation;

namespace Auth.Application.Auth.Commands.ValidateForcedPasswordChange;

public sealed class ValidateForcedPasswordChangeCommandValidator : AbstractValidator<ValidateForcedPasswordChangeCommand>
{
    public ValidateForcedPasswordChangeCommandValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}
