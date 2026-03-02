using Auth.Application;
using Auth.Domain;
using Auth.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests;

public sealed class UserServiceTests
{
    [Fact]
    public async Task GetWorkspacesAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.GetWorkspacesAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWorkspacesAsync_WhenUserHasNoWorkspaces_ReturnsEmptyCollection()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetWorkspacesAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWorkspacesAsync_WhenUserHasWorkspaceWithNoRoles_ReturnsWorkspaceWithEmptyRoleIds()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var workspace = new Workspace { Name = "system", Description = "System workspace", IsSystem = false };
        dbContext.Users.Add(user);
        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync();
        dbContext.UserWorkspaces.Add(new UserWorkspace { UserId = user.Id, WorkspaceId = workspace.Id });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetWorkspacesAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result!.Single().WorkSpaceId.Should().Be(workspace.Id);
        result.Single().RoleIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWorkspacesAsync_WhenUserHasWorkspacesWithRoles_ReturnsWorkspacesWithRoleIds()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var workspace = new Workspace { Name = "system", Description = "System workspace", IsSystem = false };
        var role = new Role { Name = "editor", Description = "Editor" };
        dbContext.Users.Add(user);
        dbContext.Workspaces.Add(workspace);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var userWorkspace = new UserWorkspace { UserId = user.Id, WorkspaceId = workspace.Id };
        dbContext.UserWorkspaces.Add(userWorkspace);
        await dbContext.SaveChangesAsync();
        dbContext.UserWorkspaceRoles.Add(new UserWorkspaceRole { UserWorkspaceId = userWorkspace.Id, RoleId = role.Id });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetWorkspacesAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result!.Single().WorkSpaceId.Should().Be(workspace.Id);
        result.Single().RoleIds.Should().ContainSingle(id => id == role.Id);
    }

    [Fact]
    public async Task GetWorkspacesAsync_WhenUserHasMultipleWorkspaces_ReturnsAll()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var ws1 = new Workspace { Name = "ws1", Description = "Workspace 1", IsSystem = false };
        var ws2 = new Workspace { Name = "ws2", Description = "Workspace 2", IsSystem = false };
        dbContext.Users.Add(user);
        dbContext.Workspaces.AddRange(ws1, ws2);
        await dbContext.SaveChangesAsync();
        dbContext.UserWorkspaces.AddRange(
            new UserWorkspace { UserId = user.Id, WorkspaceId = ws1.Id },
            new UserWorkspace { UserId = user.Id, WorkspaceId = ws2.Id });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetWorkspacesAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result!.Select(x => x.WorkSpaceId).Should().BeEquivalentTo(new[] { ws1.Id, ws2.Id });
    }

    [Fact]
    public async Task CreateAsync_WhenTwoFactorEnabled_SetsEnabledWithChannel()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        var service = CreateService(dbContext, hasher);

        var result = await service.CreateAsync(
            new CreateUserRequest("bob", "Bob", "bob@example.com", "pwd", TwoFactorEnabled: true, TwoFactorChannel: TwoFactorChannel.Email),
            CancellationToken.None);

        result.TwoFactorEnabled.Should().BeTrue();
        result.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);

        var user = await dbContext.Users.FirstAsync(x => x.Id == result.Id);
        user.TwoFactorEnabled.Should().BeTrue();
        user.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task CreateAsync_WhenTwoFactorEnabledWithoutChannel_DefaultsToEmail()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        var service = CreateService(dbContext, hasher);

        var result = await service.CreateAsync(
            new CreateUserRequest("bob", "Bob", "bob@example.com", "pwd", TwoFactorEnabled: true, TwoFactorChannel: null),
            CancellationToken.None);

        result.TwoFactorEnabled.Should().BeTrue();
        result.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task CreateAsync_WhenTwoFactorDisabled_LeavesDisabled()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        var service = CreateService(dbContext, hasher);

        var result = await service.CreateAsync(
            new CreateUserRequest("bob", "Bob", "bob@example.com", "pwd", TwoFactorEnabled: false),
            CancellationToken.None);

        result.TwoFactorEnabled.Should().BeFalse();
        result.TwoFactorChannel.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenTwoFactorEnabled_SetsEnabledWithChannel()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.UpdateAsync(
            user.Id,
            new UpdateUserRequest("alice", "Alice", "alice@example.com", null, true, TwoFactorEnabled: true, TwoFactorChannel: TwoFactorChannel.Email),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.TwoFactorEnabled.Should().BeTrue();
        result.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task UpdateAsync_WhenTwoFactorDisabled_DisablesTwoFactor()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.UpdateAsync(
            user.Id,
            new UpdateUserRequest("alice", "Alice", "alice@example.com", null, true, TwoFactorEnabled: false),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.TwoFactorEnabled.Should().BeFalse();
        result.TwoFactorChannel.Should().BeNull();
    }

    [Fact]
    public async Task PatchAsync_WhenTwoFactorEnabledWithChannel_SetsChannel()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.PatchAsync(
            user.Id,
            new PatchUserRequest(null, null, null, null, null, TwoFactorEnabled: true, TwoFactorChannel: TwoFactorChannel.Email),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.TwoFactorEnabled.Should().BeTrue();
        result.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task PatchAsync_WhenTwoFactorDisabled_DisablesTwoFactor()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.PatchAsync(
            user.Id,
            new PatchUserRequest(null, null, null, null, null, TwoFactorEnabled: false),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.TwoFactorEnabled.Should().BeFalse();
        result.TwoFactorChannel.Should().BeNull();
    }

    [Fact]
    public async Task PatchAsync_WhenTwoFactorNotSpecified_DoesNotChangeTwoFactor()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.PatchAsync(
            user.Id,
            new PatchUserRequest(null, null, "newemail@example.com", null, null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Email.Should().Be("newemail@example.com");
        result.TwoFactorEnabled.Should().BeTrue();
        result.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task CreateAsync_WithPhone_SetsPhone()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        var service = CreateService(dbContext, hasher);

        var result = await service.CreateAsync(
            new CreateUserRequest("bob", "Bob", "bob@example.com", "pwd", Phone: "+1234567890"),
            CancellationToken.None);

        result.Phone.Should().Be("+1234567890");

        var user = await dbContext.Users.FirstAsync(x => x.Id == result.Id);
        user.Phone.Should().Be("+1234567890");
    }

    [Fact]
    public async Task PatchAsync_WithPhone_UpdatesPhone()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.PatchAsync(
            user.Id,
            new PatchUserRequest(null, null, null, "+9876543210", null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Phone.Should().Be("+9876543210");
    }

    [Fact]
    public async Task PatchAsync_WithoutPhone_DoesNotChangePhone()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", Phone = "+1111111111", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.PatchAsync(
            user.Id,
            new PatchUserRequest(null, null, "newemail@example.com", null, null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Email.Should().Be("newemail@example.com");
        result.Phone.Should().Be("+1111111111");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenUserExists_UpdatesPasswordAndSetsMustChangePassword()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash("tempPass123")).Returns("hashed_temp");
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "old_hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, hasher);

        var result = await service.ResetPasswordAsync(user.Id, "tempPass123", CancellationToken.None);

        result.Should().BeTrue();
        var updated = await dbContext.Users.FirstAsync(x => x.Id == user.Id);
        updated.PasswordHash.Should().Be("hashed_temp");
        updated.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenUserDoesNotExist_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.ResetPasswordAsync(Guid.NewGuid(), "tempPass123", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetIdentitySourceLinksAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.GetIdentitySourceLinksAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetIdentitySourceLinksAsync_WhenUserHasNoLinks_ReturnsEmptyCollection()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetIdentitySourceLinksAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetIdentitySourceLinksAsync_WhenUserHasLinks_ReturnsLinksWithSourceDetails()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var source = new IdentitySource { Name = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        dbContext.Users.Add(user);
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
        {
            UserId = user.Id,
            IdentitySourceId = source.Id,
            ExternalIdentity = "ext-sub-123"
        });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetIdentitySourceLinksAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var link = result!.Single();
        link.IdentitySourceId.Should().Be(source.Id);
        link.IdentitySourceName.Should().Be("keycloak");
        link.IdentitySourceDisplayName.Should().Be("Keycloak");
        link.IdentitySourceType.Should().Be(IdentitySourceType.Oidc);
        link.ExternalIdentity.Should().Be("ext-sub-123");
    }

    [Fact]
    public async Task GetIdentitySourceLinksAsync_WhenUserHasMultipleLinks_ReturnsAll()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@example.com", PasswordHash = "hash", IsActive = true };
        var source1 = new IdentitySource { Name = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        var source2 = new IdentitySource { Name = "corporate-ldap", DisplayName = "Corporate LDAP", Type = IdentitySourceType.Ldap, IsEnabled = true };
        dbContext.Users.Add(user);
        dbContext.IdentitySources.AddRange(source1, source2);
        await dbContext.SaveChangesAsync();
        dbContext.IdentitySourceLinks.AddRange(
            new IdentitySourceLink { UserId = user.Id, IdentitySourceId = source1.Id, ExternalIdentity = "oidc-sub" },
            new IdentitySourceLink { UserId = user.Id, IdentitySourceId = source2.Id, ExternalIdentity = "ldap-uid" });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.GetIdentitySourceLinksAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result!.Select(x => x.IdentitySourceName).Should().BeEquivalentTo("keycloak", "corporate-ldap");
    }

    private static UserService CreateService(
        AuthDbContext dbContext,
        Mock<IPasswordHasher>? passwordHasher = null,
        Mock<ISearchIndexService>? searchIndexService = null)
    {
        passwordHasher ??= new Mock<IPasswordHasher>();
        searchIndexService ??= new Mock<ISearchIndexService>();
        return new UserService(dbContext, passwordHasher.Object, searchIndexService.Object);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
