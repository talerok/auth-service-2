using Auth.Domain;
using MediatR;

namespace Auth.Application.Auth.Commands.ValidateCredentials;

public sealed record ValidateCredentialsCommand(string Username, string Password) : IRequest<User>;
