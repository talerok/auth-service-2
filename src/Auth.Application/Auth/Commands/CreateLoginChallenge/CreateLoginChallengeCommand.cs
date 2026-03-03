using Auth.Domain;
using MediatR;

namespace Auth.Application.Auth.Commands.CreateLoginChallenge;

public sealed record CreateLoginChallengeCommand(Guid UserId, TwoFactorChannel Channel) : IRequest<TwoFactorChallenge>;
