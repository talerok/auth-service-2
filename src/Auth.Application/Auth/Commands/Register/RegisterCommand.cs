using MediatR;

namespace Auth.Application.Auth.Commands.Register;

public sealed record RegisterCommand(string Username, string FullName, string Email, string Password) : IRequest<UserDto>;
