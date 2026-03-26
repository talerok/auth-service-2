using FluentValidation;

namespace Auth.Application.Verification.Commands.ConfirmEmailVerification;

public sealed class ConfirmEmailVerificationCommandValidator : AbstractValidator<ConfirmEmailVerificationCommand>
{
    public ConfirmEmailVerificationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.Otp).NotEmpty().MaximumLength(16);
    }
}
