using Auth.Application;
using Auth.Application.IdentitySources.Commands.CreateIdentitySourceLink;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.IdentitySources.Commands.CreateIdentitySourceLink;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.UnitTests.IdentitySources.Commands;

public sealed class CreateIdentitySourceLinkCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesLink()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource { Name = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        var user = new User { Username = "testuser", Email = "test@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var handler = new CreateIdentitySourceLinkCommandHandler(dbContext);

        var result = await handler.Handle(
            new CreateIdentitySourceLinkCommand(source.Id, user.Id, "ext-sub-123"),
            CancellationToken.None);

        result.ExternalIdentity.Should().Be("ext-sub-123");
        result.UserId.Should().Be(user.Id);
        (await dbContext.IdentitySourceLinks.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_DuplicateLink_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource { Name = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        var user = new User { Username = "testuser", Email = "test@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
        {
            UserId = user.Id, IdentitySourceId = source.Id, ExternalIdentity = "ext-sub-123"
        });
        await dbContext.SaveChangesAsync();
        var handler = new CreateIdentitySourceLinkCommandHandler(dbContext);

        var act = () => handler.Handle(
            new CreateIdentitySourceLinkCommand(source.Id, user.Id, "ext-sub-123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceDuplicateLink);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
