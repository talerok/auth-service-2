using FluentValidation;
using Microsoft.Extensions.Options;

namespace Auth.Application.Auth.Commands.ValidateForcedPasswordChange;

public sealed class ValidateForcedPasswordChangeCommandValidator : AbstractValidator<ValidateForcedPasswordChangeCommand>
{
    public ValidateForcedPasswordChangeCommandValidator(IOptions<PasswordRequirementsOptions> passwordOptions)
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.NewPassword).MeetsPasswordRequirements(passwordOptions.Value);
    }
}
