using FluentValidation;

namespace Auth.Application.Verification.Commands.SendPhoneVerification;

public sealed class SendPhoneVerificationCommandValidator : AbstractValidator<SendPhoneVerificationCommand>
{
    public SendPhoneVerificationCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
