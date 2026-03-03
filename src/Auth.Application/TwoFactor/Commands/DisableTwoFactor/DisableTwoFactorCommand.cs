using MediatR;

namespace Auth.Application.TwoFactor.Commands.DisableTwoFactor;

public sealed record DisableTwoFactorCommand(Guid UserId) : IRequest;
