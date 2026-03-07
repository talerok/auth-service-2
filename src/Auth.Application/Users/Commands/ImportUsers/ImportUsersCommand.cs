using MediatR;

namespace Auth.Application.Users.Commands.ImportUsers;

public sealed record ImportUsersCommand(
    IReadOnlyCollection<ImportUserItem> Items,
    bool Add = true,
    bool Edit = true,
    bool BlockMissing = false) : IRequest<ImportUsersResult>;
