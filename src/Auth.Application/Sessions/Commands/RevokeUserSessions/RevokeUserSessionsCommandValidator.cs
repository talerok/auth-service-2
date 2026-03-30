using FluentValidation;

namespace Auth.Application.Sessions.Commands.RevokeUserSessions;

public sealed class RevokeUserSessionsCommandValidator : AbstractValidator<RevokeUserSessionsCommand>
{
    public RevokeUserSessionsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(100);
    }
}
