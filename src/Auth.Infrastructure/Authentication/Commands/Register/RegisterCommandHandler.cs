using Auth.Application;
using Auth.Application.Auth.Commands.Register;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Authentication.Commands.Register;

internal sealed class RegisterCommandHandler(
    AuthDbContext dbContext,
    IPasswordHasher passwordHasher,
    ISearchIndexService searchIndexService) : IRequestHandler<RegisterCommand, UserDto>
{
    public async Task<UserDto> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AnyAsync(
            x => x.Username == command.Username || x.Email == command.Email, cancellationToken);
        if (exists)
        {
            throw new AuthException(AuthErrorCatalog.DuplicateIdentity);
        }

        var user = new User
        {
            Username = command.Username,
            FullName = command.FullName,
            Email = command.Email,
            PasswordHash = passwordHasher.Hash(command.Password),
            IsActive = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Phone, user.IsActive, user.MustChangePassword, user.TwoFactorEnabled, user.TwoFactorChannel);
        await searchIndexService.IndexUserAsync(dto, cancellationToken);
        return dto;
    }
}
