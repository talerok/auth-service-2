using FluentValidation;

namespace Auth.Application.Verification.Commands.SendEmailVerification;

public sealed class SendEmailVerificationCommandValidator : AbstractValidator<SendEmailVerificationCommand>
{
    public SendEmailVerificationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
