using MediatR;

namespace Auth.Application.Users.Queries.ExportUsers;

public sealed record ExportUsersQuery : IRequest<IReadOnlyCollection<ExportUserDto>>;
