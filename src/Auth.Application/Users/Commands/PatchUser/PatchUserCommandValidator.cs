using FluentValidation;

namespace Auth.Application.Users.Commands.PatchUser;

public sealed class PatchUserCommandValidator : AbstractValidator<PatchUserCommand>
{
    public PatchUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Username.Value!).NotNull().MaximumLength(100)
            .OverridePropertyName("Username").When(x => x.Username.HasValue);
        RuleFor(x => x.FullName.Value!).NotNull().MaximumLength(200)
            .OverridePropertyName("FullName").When(x => x.FullName.HasValue);
        RuleFor(x => x.Email.Value!).NotNull().EmailAddress()
            .OverridePropertyName("Email").When(x => x.Email.HasValue);
        RuleFor(x => x.Locale.Value!).NotNull()
            .OverridePropertyName("Locale").When(x => x.Locale.HasValue);
    }
}
