using Auth.Application;
using Auth.Application.Users.Commands.SetUserIdentitySourceLinks;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Users.Commands.SetUserIdentitySourceLinks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Users.Commands;

public sealed class SetUserIdentitySourceLinksCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsNewLinks()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var source = new IdentitySource { Name = "keycloak", Code = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        dbContext.Users.Add(user);
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();
        var handler = new SetUserIdentitySourceLinksCommandHandler(dbContext, new Mock<IAuditContext>().Object);

        await handler.Handle(new SetUserIdentitySourceLinksCommand(user.Id,
            [new UserIdentitySourceLinkItem(source.Id, "ext-sub-123")]), CancellationToken.None);

        var links = await dbContext.IdentitySourceLinks.Where(x => x.UserId == user.Id).ToListAsync();
        links.Should().HaveCount(1);
        links[0].IdentitySourceId.Should().Be(source.Id);
        links[0].ExternalIdentity.Should().Be("ext-sub-123");
    }

    [Fact]
    public async Task Handle_RemovesUnlistedLinks()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var source1 = new IdentitySource { Name = "keycloak", Code = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        var source2 = new IdentitySource { Name = "ldap", Code = "ldap", DisplayName = "LDAP", Type = IdentitySourceType.Ldap, IsEnabled = true };
        dbContext.Users.Add(user);
        dbContext.IdentitySources.AddRange(source1, source2);
        dbContext.IdentitySourceLinks.AddRange(
            new IdentitySourceLink { UserId = user.Id, IdentitySourceId = source1.Id, ExternalIdentity = "ext1" },
            new IdentitySourceLink { UserId = user.Id, IdentitySourceId = source2.Id, ExternalIdentity = "ext2" });
        await dbContext.SaveChangesAsync();
        var handler = new SetUserIdentitySourceLinksCommandHandler(dbContext, new Mock<IAuditContext>().Object);

        await handler.Handle(new SetUserIdentitySourceLinksCommand(user.Id,
            [new UserIdentitySourceLinkItem(source1.Id, "ext1")]), CancellationToken.None);

        var links = await dbContext.IdentitySourceLinks.Where(x => x.UserId == user.Id).ToListAsync();
        links.Should().HaveCount(1);
        links[0].IdentitySourceId.Should().Be(source1.Id);
    }

    [Fact]
    public async Task Handle_UpdatesExternalIdentity()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var source = new IdentitySource { Name = "keycloak", Code = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        dbContext.Users.Add(user);
        dbContext.IdentitySources.Add(source);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink { UserId = user.Id, IdentitySourceId = source.Id, ExternalIdentity = "old-ext" });
        await dbContext.SaveChangesAsync();
        var handler = new SetUserIdentitySourceLinksCommandHandler(dbContext, new Mock<IAuditContext>().Object);

        await handler.Handle(new SetUserIdentitySourceLinksCommand(user.Id,
            [new UserIdentitySourceLinkItem(source.Id, "new-ext")]), CancellationToken.None);

        var links = await dbContext.IdentitySourceLinks.Where(x => x.UserId == user.Id).ToListAsync();
        links.Should().HaveCount(1);
        links[0].ExternalIdentity.Should().Be("new-ext");
    }

    [Fact]
    public async Task Handle_EmptyList_RemovesAllLinks()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var source = new IdentitySource { Name = "keycloak", Code = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        dbContext.Users.Add(user);
        dbContext.IdentitySources.Add(source);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink { UserId = user.Id, IdentitySourceId = source.Id, ExternalIdentity = "ext" });
        await dbContext.SaveChangesAsync();
        var handler = new SetUserIdentitySourceLinksCommandHandler(dbContext, new Mock<IAuditContext>().Object);

        await handler.Handle(new SetUserIdentitySourceLinksCommand(user.Id, []), CancellationToken.None);

        var links = await dbContext.IdentitySourceLinks.Where(x => x.UserId == user.Id).ToListAsync();
        links.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DoesNotAffectOtherUsersLinks()
    {
        await using var dbContext = CreateDbContext();
        var user1 = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var user2 = new User { Username = "bob", Email = "bob@example.com", PasswordHash = "hash", IsActive = true };
        var source = new IdentitySource { Name = "keycloak", Code = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        dbContext.Users.AddRange(user1, user2);
        dbContext.IdentitySources.Add(source);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink { UserId = user2.Id, IdentitySourceId = source.Id, ExternalIdentity = "bob-ext" });
        await dbContext.SaveChangesAsync();
        var handler = new SetUserIdentitySourceLinksCommandHandler(dbContext, new Mock<IAuditContext>().Object);

        await handler.Handle(new SetUserIdentitySourceLinksCommand(user1.Id,
            [new UserIdentitySourceLinkItem(source.Id, "alice-ext")]), CancellationToken.None);

        var allLinks = await dbContext.IdentitySourceLinks.ToListAsync();
        allLinks.Should().HaveCount(2);
        allLinks.Should().Contain(x => x.UserId == user2.Id && x.ExternalIdentity == "bob-ext");
    }

}
