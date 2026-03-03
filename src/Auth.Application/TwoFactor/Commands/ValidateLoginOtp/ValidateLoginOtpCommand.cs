using Auth.Domain;
using MediatR;

namespace Auth.Application.TwoFactor.Commands.ValidateLoginOtp;

public sealed record ValidateLoginOtpCommand(
    Guid ChallengeId,
    TwoFactorChannel Channel,
    string Otp) : IRequest<User>;
