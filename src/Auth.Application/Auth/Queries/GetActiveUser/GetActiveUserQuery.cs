using Auth.Domain;
using MediatR;

namespace Auth.Application.Auth.Queries.GetActiveUser;

public sealed record GetActiveUserQuery(Guid UserId) : IRequest<User>;
