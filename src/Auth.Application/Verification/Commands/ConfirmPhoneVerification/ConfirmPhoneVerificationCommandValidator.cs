using FluentValidation;

namespace Auth.Application.Verification.Commands.ConfirmPhoneVerification;

public sealed class ConfirmPhoneVerificationCommandValidator : AbstractValidator<ConfirmPhoneVerificationCommand>
{
    public ConfirmPhoneVerificationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.Otp).NotEmpty().MaximumLength(16);
    }
}
