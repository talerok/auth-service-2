using FluentValidation;
using Microsoft.Extensions.Options;

namespace Auth.Application.Users.Commands.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator(IOptions<PasswordRequirementsOptions> passwordOptions)
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewPassword).MeetsPasswordRequirements(passwordOptions.Value);
    }
}
