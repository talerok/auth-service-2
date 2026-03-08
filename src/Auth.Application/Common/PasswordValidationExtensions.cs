using FluentValidation;

namespace Auth.Application;

public static class PasswordValidationExtensions
{
    public static IRuleBuilderOptions<T, string> MeetsPasswordRequirements<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        PasswordRequirementsOptions options)
    {
        return ruleBuilder
            .NotEmpty()
            .MinimumLength(options.MinLength)
            .MaximumLength(options.MaxLength)
            .Must(p => !options.RequireUppercase || p.Any(char.IsUpper))
                .WithMessage($"Password must contain at least one uppercase letter.")
            .Must(p => !options.RequireLowercase || p.Any(char.IsLower))
                .WithMessage($"Password must contain at least one lowercase letter.")
            .Must(p => !options.RequireDigit || p.Any(char.IsDigit))
                .WithMessage($"Password must contain at least one digit.")
            .Must(p => !options.RequireSpecialCharacter || p.Any(c => !char.IsLetterOrDigit(c)))
                .WithMessage($"Password must contain at least one special character.");
    }
}
