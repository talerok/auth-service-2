using FluentValidation;

namespace Auth.Application.Sessions.Commands.RevokeSession;

public sealed class RevokeSessionCommandValidator : AbstractValidator<RevokeSessionCommand>
{
    public RevokeSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(100);
    }
}
