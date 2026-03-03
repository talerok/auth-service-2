using Auth.Domain;
using MediatR;

namespace Auth.Application.TwoFactor.Commands.EnableTwoFactor;

public sealed record EnableTwoFactorCommand(
    Guid UserId,
    TwoFactorChannel Channel,
    bool IsHighRisk = false) : IRequest<EnableTwoFactorResponse>;
