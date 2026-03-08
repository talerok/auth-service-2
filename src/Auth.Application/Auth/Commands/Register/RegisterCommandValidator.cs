using FluentValidation;
using Microsoft.Extensions.Options;

namespace Auth.Application.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator(IOptions<PasswordRequirementsOptions> passwordOptions)
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MeetsPasswordRequirements(passwordOptions.Value);
    }
}
