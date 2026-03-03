using Auth.Domain;
using MediatR;

namespace Auth.Application.Auth.Commands.CreatePasswordChangeChallenge;

public sealed record CreatePasswordChangeChallengeCommand(Guid UserId) : IRequest<PasswordChangeChallenge>;
