using Auth.Application;
using Auth.Application.Users.Commands.ImportUsers;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Users.Commands.ImportUsers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests.Users.Commands;

public sealed class ImportUsersCommandHandlerTests
{
    [Fact]
    public async Task Import_CreatesNewUser_WithTemporaryPasswordAndMustChangePassword()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john", "John Doe", "john@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        var item = result.Items.First();
        item.Status.Should().Be("created");
        item.Username.Should().Be("john");
        item.TemporaryPassword.Should().NotBeNullOrEmpty();
        item.TemporaryPassword.Should().HaveLength(16);
        item.Error.Should().BeNull();

        var user = await dbContext.Users.FirstAsync(u => u.Username == "john");
        user.FullName.Should().Be("John Doe");
        user.Email.Should().Be("john@example.com");
        user.MustChangePassword.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Import_UpdatesExistingUser()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "john", FullName = "Old Name", Email = "old@example.com",
            PasswordHash = "hash", IsActive = true, IsInternalAuthEnabled = true
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john", "John Updated", "john@example.com", "+123", true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        var item = result.Items.First();
        item.Status.Should().Be("updated");
        item.TemporaryPassword.Should().BeNull();
        item.Error.Should().BeNull();

        var user = await dbContext.Users.FirstAsync(u => u.Username == "john");
        user.FullName.Should().Be("John Updated");
        user.Phone.Should().Be("+123");
    }

    [Fact]
    public async Task Import_MixedCreateAndUpdate_ReturnsCorrectStatuses()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "existing", FullName = "Existing", Email = "existing@example.com",
            PasswordHash = "hash"
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("existing", "Existing Updated", "existing@example.com", null, true, true, false, false, null, null, null),
            new("newuser", "New User", "new@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.First(x => x.Username == "existing").Status.Should().Be("updated");
        result.Items.First(x => x.Username == "newuser").Status.Should().Be("created");
        result.Items.First(x => x.Username == "newuser").TemporaryPassword.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Import_AddFalse_SkipsNewUsers()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "existing", FullName = "Existing", Email = "existing@example.com",
            PasswordHash = "hash"
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("existing", "Updated", "existing@example.com", null, true, true, false, false, null, null, null),
            new("newuser", "New", "new@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items, Add: false), CancellationToken.None);

        result.Items.First(x => x.Username == "existing").Status.Should().Be("updated");
        result.Items.First(x => x.Username == "newuser").Status.Should().Be("skipped");
        (await dbContext.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Import_EditFalse_SkipsExistingUsers()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "existing", FullName = "Old", Email = "existing@example.com",
            PasswordHash = "hash"
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("existing", "Updated", "existing@example.com", null, true, true, false, false, null, null, null),
            new("newuser", "New", "new@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items, Edit: false), CancellationToken.None);

        result.Items.First(x => x.Username == "existing").Status.Should().Be("skipped");
        result.Items.First(x => x.Username == "newuser").Status.Should().Be("created");
        var existing = await dbContext.Users.FirstAsync(u => u.Username == "existing");
        existing.FullName.Should().Be("Old");
    }

    [Fact]
    public async Task Import_BothFalse_SkipsAll()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "existing", FullName = "Old", Email = "existing@example.com",
            PasswordHash = "hash"
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("existing", "Updated", "existing@example.com", null, true, true, false, false, null, null, null),
            new("newuser", "New", "new@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items, Add: false, Edit: false), CancellationToken.None);

        result.Items.Should().AllSatisfy(x => x.Status.Should().Be("skipped"));
    }

    [Fact]
    public async Task Import_RestoresSoftDeletedUser()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "deleted", FullName = "Deleted", Email = "deleted@example.com",
            PasswordHash = "hash", DeletedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("deleted", "Restored", "deleted@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First().Status.Should().Be("updated");
        var user = await dbContext.Users.IgnoreQueryFilters().FirstAsync(u => u.Username == "deleted");
        user.DeletedAt.Should().BeNull();
        user.FullName.Should().Be("Restored");
    }

    [Fact]
    public async Task Import_InvalidEmail_ReturnsError()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john", "John", "not-an-email", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First().Status.Should().Be("error");
        result.Items.First().Error.Should().Be(AuthErrorCatalog.ImportUserInvalidEmail);
    }

    [Fact]
    public async Task Import_EmptyUsername_ReturnsError()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("", "John", "john@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First().Status.Should().Be("error");
        result.Items.First().Error.Should().Be(AuthErrorCatalog.ImportUserInvalidUsername);
    }

    [Fact]
    public async Task Import_EmptyFullName_ReturnsError()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john", "", "john@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First().Status.Should().Be("error");
        result.Items.First().Error.Should().Be(AuthErrorCatalog.ImportUserInvalidFullName);
    }

    [Fact]
    public async Task Import_DuplicateUsernameInFile_ReturnsError()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john", "John 1", "john1@example.com", null, true, true, false, false, null, null, null),
            new("john", "John 2", "john2@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First(x => x.Status == "created").Username.Should().Be("john");
        result.Items.First(x => x.Status == "error").Error.Should().Be(AuthErrorCatalog.ImportUserDuplicateUsername);
    }

    [Fact]
    public async Task Import_DuplicateEmailInFile_ReturnsError()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john1", "John 1", "same@example.com", null, true, true, false, false, null, null, null),
            new("john2", "John 2", "same@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First(x => x.Status == "created").Username.Should().Be("john1");
        result.Items.First(x => x.Status == "error").Error.Should().Be(AuthErrorCatalog.ImportUserDuplicateEmail);
    }

    [Fact]
    public async Task Import_EmailConflictWithExistingUser_ReturnsError()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "alice", FullName = "Alice", Email = "taken@example.com",
            PasswordHash = "hash"
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("bob", "Bob", "taken@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First().Status.Should().Be("error");
        result.Items.First().Error.Should().Be(AuthErrorCatalog.ImportUserEmailConflict);
    }

    [Fact]
    public async Task Import_WorkspaceNotFound_ReturnsError()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john", "John", "john@example.com", null, true, true, false, false, null,
                [new ImportUserWorkspaceItem("nonexistent", ["admin"])], null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First().Status.Should().Be("error");
        result.Items.First().Error.Should().Be(AuthErrorCatalog.ImportUserWorkspaceNotFound);
    }

    [Fact]
    public async Task Import_RoleNotFound_ReturnsError()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Workspaces.Add(new Workspace { Name = "Dev", Code = "dev", Description = "Dev" });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john", "John", "john@example.com", null, true, true, false, false, null,
                [new ImportUserWorkspaceItem("dev", ["nonexistent-role"])], null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First().Status.Should().Be("error");
        result.Items.First().Error.Should().Be(AuthErrorCatalog.ImportUserRoleNotFound);
    }

    [Fact]
    public async Task Import_IdentitySourceNotFound_ReturnsError()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john", "John", "john@example.com", null, true, true, false, false, null,
                null, [new ImportUserIdentitySourceItem("nonexistent-source", "ext-id")])
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First().Status.Should().Be("error");
        result.Items.First().Error.Should().Be(AuthErrorCatalog.ImportUserIdentitySourceNotFound);
    }

    [Fact]
    public async Task Import_WithWorkspacesAndRoles_AssignsCorrectly()
    {
        await using var dbContext = CreateDbContext();
        var workspace = new Workspace { Name = "Dev", Code = "dev", Description = "Dev" };
        var role = new Role { Name = "Admin", Code = "admin", Description = "Admin" };
        dbContext.Workspaces.Add(workspace);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john", "John", "john@example.com", null, true, true, false, false, null,
                [new ImportUserWorkspaceItem("dev", ["admin"])], null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First().Status.Should().Be("created");
        var user = await dbContext.Users.FirstAsync(u => u.Username == "john");
        var uw = await dbContext.UserWorkspaces.FirstAsync(x => x.UserId == user.Id);
        uw.WorkspaceId.Should().Be(workspace.Id);
        var uwr = await dbContext.UserWorkspaceRoles.FirstAsync(x => x.UserWorkspaceId == uw.Id);
        uwr.RoleId.Should().Be(role.Id);
    }

    [Fact]
    public async Task Import_WithIdentitySources_AssignsCorrectly()
    {
        await using var dbContext = CreateDbContext();
        var identitySource = new IdentitySource
        {
            Name = "Corp LDAP", Code = "corp-ldap", DisplayName = "Corporate LDAP",
            Type = IdentitySourceType.Ldap
        };
        dbContext.IdentitySources.Add(identitySource);
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john", "John", "john@example.com", null, true, true, false, false, null,
                null, [new ImportUserIdentitySourceItem("corp-ldap", "uid=john,dc=corp")])
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First().Status.Should().Be("created");
        var user = await dbContext.Users.FirstAsync(u => u.Username == "john");
        var link = await dbContext.IdentitySourceLinks.FirstAsync(l => l.UserId == user.Id);
        link.IdentitySourceId.Should().Be(identitySource.Id);
        link.ExternalIdentity.Should().Be("uid=john,dc=corp");
    }

    [Fact]
    public async Task Import_BlockMissing_DeactivatesAbsentUsers()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "existing", FullName = "Existing", Email = "existing@example.com",
            PasswordHash = "hash", IsActive = true
        });
        dbContext.Users.Add(new User
        {
            Username = "absent", FullName = "Absent", Email = "absent@example.com",
            PasswordHash = "hash", IsActive = true
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("existing", "Existing Updated", "existing@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items, BlockMissing: true), CancellationToken.None);

        result.Blocked.Should().Be(1);
        var absent = await dbContext.Users.FirstAsync(u => u.Username == "absent");
        absent.IsActive.Should().BeFalse();
        var existing = await dbContext.Users.FirstAsync(u => u.Username == "existing");
        existing.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Import_BlockMissing_DoesNotDeactivateImportedUsers()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "existing", FullName = "Existing", Email = "existing@example.com",
            PasswordHash = "hash", IsActive = true
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("existing", "Updated", "existing@example.com", null, true, true, false, false, null, null, null),
            new("newuser", "New", "new@example.com", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items, BlockMissing: true), CancellationToken.None);

        result.Blocked.Should().Be(0);
        (await dbContext.Users.CountAsync(u => u.IsActive)).Should().Be(2);
    }

    [Fact]
    public async Task Import_UpdateExistingUser_SyncsWorkspaces()
    {
        await using var dbContext = CreateDbContext();
        var wsOld = new Workspace { Name = "Old", Code = "old", Description = "Old" };
        var wsNew = new Workspace { Name = "New", Code = "new", Description = "New" };
        var role = new Role { Name = "Admin", Code = "admin", Description = "Admin" };
        dbContext.Workspaces.AddRange(wsOld, wsNew);
        dbContext.Roles.Add(role);
        var user = new User
        {
            Username = "john", FullName = "John", Email = "john@example.com",
            PasswordHash = "hash"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var uw = new UserWorkspace { UserId = user.Id, WorkspaceId = wsOld.Id };
        dbContext.UserWorkspaces.Add(uw);
        await dbContext.SaveChangesAsync();
        dbContext.UserWorkspaceRoles.Add(new UserWorkspaceRole { UserWorkspaceId = uw.Id, RoleId = role.Id });
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john", "John", "john@example.com", null, true, true, false, false, null,
                [new ImportUserWorkspaceItem("new", ["admin"])], null)
        };
        await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        var userWorkspaces = await dbContext.UserWorkspaces.Where(x => x.UserId == user.Id).ToListAsync();
        userWorkspaces.Should().HaveCount(1);
        userWorkspaces.First().WorkspaceId.Should().Be(wsNew.Id);
    }

    [Fact]
    public async Task Import_CreatedUser_HasTwoFactorEnabled()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("john", "John", "john@example.com", null, true, true, false, true, TwoFactorChannel.Email, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First().Status.Should().Be("created");
        var user = await dbContext.Users.FirstAsync(u => u.Username == "john");
        user.TwoFactorEnabled.Should().BeTrue();
        user.TwoFactorChannel.Should().Be(TwoFactorChannel.Email);
    }

    [Fact]
    public async Task Import_ErrorItemDoesNotAffectValidItems()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var items = new List<ImportUserItem>
        {
            new("valid", "Valid User", "valid@example.com", null, true, true, false, false, null, null, null),
            new("invalid", "Invalid", "not-email", null, true, true, false, false, null, null, null)
        };
        var result = await handler.Handle(new ImportUsersCommand(items), CancellationToken.None);

        result.Items.First(x => x.Username == "valid").Status.Should().Be("created");
        result.Items.First(x => x.Username == "invalid").Status.Should().Be("error");
        (await dbContext.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public void GenerateTemporaryPassword_Returns16CharString()
    {
        var password = ImportUsersCommandHandler.GenerateTemporaryPassword();

        password.Should().HaveLength(16);
        password.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateTemporaryPassword_ReturnsDifferentPasswords()
    {
        var passwords = Enumerable.Range(0, 10)
            .Select(_ => ImportUsersCommandHandler.GenerateTemporaryPassword())
            .ToList();

        passwords.Distinct().Count().Should().Be(10);
    }

    private static ImportUsersCommandHandler CreateHandler(AuthDbContext dbContext)
    {
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed");
        var searchIndex = new Mock<ISearchIndexService>();
        return new ImportUsersCommandHandler(dbContext, passwordHasher.Object, searchIndex.Object);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
