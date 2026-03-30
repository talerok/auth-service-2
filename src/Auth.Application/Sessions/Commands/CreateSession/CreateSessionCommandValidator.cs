using FluentValidation;

namespace Auth.Application.Sessions.Commands.CreateSession;

public sealed class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AuthMethod).NotEmpty().MaximumLength(32);
        RuleFor(x => x.IpAddress).NotEmpty().MaximumLength(45);
        RuleFor(x => x.UserAgent).NotEmpty().MaximumLength(500);
    }
}
