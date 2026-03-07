using Auth.Application;
using Auth.Application.Users.Commands.CreateUser;
using Auth.Domain;
using MediatR;

namespace Auth.Infrastructure.Users.Commands.CreateUser;

internal sealed class CreateUserCommandHandler(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher,
    ISearchIndexService searchIndexService) : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Username = command.Username,
            FullName = command.FullName,
            Email = command.Email,
            Phone = command.Phone,
            PasswordHash = passwordHasher.Hash(command.Password),
            IsActive = command.IsActive,
            IsInternalAuthEnabled = command.IsInternalAuthEnabled
        };

        if (command.MustChangePassword)
            user.MarkMustChangePassword();

        if (command.TwoFactorEnabled)
            user.EnableTwoFactor(command.TwoFactorChannel ?? TwoFactorChannel.Email);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone,
            user.IsActive, user.IsInternalAuthEnabled, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel);

        await searchIndexService.IndexUserAsync(dto, cancellationToken);
        return dto;
    }
}
