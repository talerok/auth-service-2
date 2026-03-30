using FluentValidation;

namespace Auth.Application.Sessions.Commands.RevokeOwnSession;

public sealed class RevokeOwnSessionCommandValidator : AbstractValidator<RevokeOwnSessionCommand>
{
    public RevokeOwnSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
