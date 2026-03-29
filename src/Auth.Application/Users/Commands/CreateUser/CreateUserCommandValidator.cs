using FluentValidation;
using Microsoft.Extensions.Options;

namespace Auth.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IOptions<PasswordRequirementsOptions> passwordOptions)
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MeetsPasswordRequirements(passwordOptions.Value);
        RuleFor(x => x.PasswordMaxAgeDays)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PasswordMaxAgeDays.HasValue);
    }
}
