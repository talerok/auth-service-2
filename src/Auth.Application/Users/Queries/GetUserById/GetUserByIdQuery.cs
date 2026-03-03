using MediatR;

namespace Auth.Application.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserDto?>;
