using Auth.Domain;
using MediatR;

namespace Auth.Application.Auth.Commands.ValidateForcedPasswordChange;

public sealed record ValidateForcedPasswordChangeCommand(Guid ChallengeId, string NewPassword) : IRequest<User>;
