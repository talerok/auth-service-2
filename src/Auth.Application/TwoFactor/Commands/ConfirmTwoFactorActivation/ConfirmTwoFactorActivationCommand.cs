using Auth.Domain;
using MediatR;

namespace Auth.Application.TwoFactor.Commands.ConfirmTwoFactorActivation;

public sealed record ConfirmTwoFactorActivationCommand(
    Guid UserId,
    Guid ChallengeId,
    TwoFactorChannel Channel,
    string Otp) : IRequest;
