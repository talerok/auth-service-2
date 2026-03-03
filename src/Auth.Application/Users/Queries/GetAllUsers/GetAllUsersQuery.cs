using MediatR;

namespace Auth.Application.Users.Queries.GetAllUsers;

public sealed record GetAllUsersQuery : IRequest<IReadOnlyCollection<UserDto>>;
