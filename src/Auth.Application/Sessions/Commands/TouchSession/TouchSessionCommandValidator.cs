using FluentValidation;

namespace Auth.Application.Sessions.Commands.TouchSession;

public sealed class TouchSessionCommandValidator : AbstractValidator<TouchSessionCommand>
{
    public TouchSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
