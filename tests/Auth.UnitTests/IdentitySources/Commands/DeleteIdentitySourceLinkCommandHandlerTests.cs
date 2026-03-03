using Auth.Application;
using Auth.Application.IdentitySources.Commands.DeleteIdentitySourceLink;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.IdentitySources.Commands.DeleteIdentitySourceLink;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.IdentitySources.Commands;

public sealed class DeleteIdentitySourceLinkCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovesLink()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource { Name = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        var user = new User { Username = "testuser", Email = "test@example.com", PasswordHash = "hash", IsActive = true };
        var link = new IdentitySourceLink { UserId = user.Id, IdentitySourceId = source.Id, ExternalIdentity = "ext-sub-123" };
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(link);
        await dbContext.SaveChangesAsync();
        var handler = new DeleteIdentitySourceLinkCommandHandler(dbContext);

        await handler.Handle(new DeleteIdentitySourceLinkCommand(source.Id, link.Id), CancellationToken.None);

        (await dbContext.IdentitySourceLinks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource { Name = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();
        var handler = new DeleteIdentitySourceLinkCommandHandler(dbContext);

        var act = () => handler.Handle(new DeleteIdentitySourceLinkCommand(source.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceLinkNotFound);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
