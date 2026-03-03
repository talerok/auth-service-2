using FluentValidation;

namespace Auth.Application.Users.Commands.PatchUser;

public sealed class PatchUserCommandValidator : AbstractValidator<PatchUserCommand>
{
    public PatchUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Username).MaximumLength(100).When(x => x.Username is not null);
        RuleFor(x => x.FullName).MaximumLength(200).When(x => x.FullName is not null);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
    }
}
