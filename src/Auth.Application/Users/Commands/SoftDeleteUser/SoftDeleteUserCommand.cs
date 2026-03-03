using MediatR;

namespace Auth.Application.Users.Commands.SoftDeleteUser;

public sealed record SoftDeleteUserCommand(Guid Id) : IRequest<bool>;
