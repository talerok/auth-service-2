using Auth.Domain;
using Auth.Infrastructure.AuditLogs;
using FluentAssertions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.AuditLogs;

public sealed class AuditDiffTests
{
    [Fact]
    public async Task CaptureState_ReturnsOnlyAuditableProperties()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "jdoe",
            FullName = "John Doe",
            Email = "john@example.com",
            Phone = "+1234567890",
            PasswordHash = "secret-hash",
            IsActive = true,
            IsInternalAuthEnabled = false
        };
        dbContext.Users.Add(user);

        var entry = dbContext.Entry(user);
        var state = AuditDiff.CaptureState(entry);

        state.Should().ContainKey("username").WhoseValue.Should().Be("jdoe");
        state.Should().ContainKey("fullName").WhoseValue.Should().Be("John Doe");
        state.Should().ContainKey("email").WhoseValue.Should().Be("john@example.com");
        state.Should().ContainKey("phone").WhoseValue.Should().Be("+1234567890");
        state.Should().ContainKey("isActive").WhoseValue.Should().Be(true);
        state.Should().ContainKey("isInternalAuthEnabled").WhoseValue.Should().Be(false);
        state.Should().ContainKey("mustChangePassword");
        state.Should().ContainKey("twoFactorEnabled");
        state.Should().ContainKey("twoFactorChannel");

        state.Should().NotContainKey("passwordHash");
        state.Should().NotContainKey("PasswordHash");
    }

    [Fact]
    public async Task CaptureState_EmptyEntity_ReturnsEmptyIfNoAuditableFields()
    {
        await using var dbContext = CreateDbContext();
        var auditLogEntry = new AuditLogEntry
        {
            ActorType = AuditActorType.User,
            EntityType = AuditEntityType.User,
            EntityId = Guid.NewGuid(),
            Action = AuditAction.Create
        };
        dbContext.AuditLogEntries.Add(auditLogEntry);

        var entry = dbContext.Entry(auditLogEntry);
        var state = AuditDiff.CaptureState(entry);

        state.Should().BeEmpty();
    }

    [Fact]
    public async Task CaptureChanges_ModifiedAuditableField_ReturnsDiff()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "jdoe",
            FullName = "OldName",
            Email = "john@example.com",
            PasswordHash = "hash"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        user.FullName = "NewName";
        dbContext.Entry(user).DetectChanges();

        var entry = dbContext.Entry(user);
        var changes = AuditDiff.CaptureChanges(entry);

        changes.Should().ContainKey("fullName");
        var diff = changes["fullName"].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        diff["old"].Should().Be("OldName");
        diff["new"].Should().Be("NewName");
    }

    [Fact]
    public async Task CaptureChanges_ModifiedNonAuditableField_ExcludesIt()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "jdoe",
            FullName = "John Doe",
            Email = "john@example.com",
            PasswordHash = "old-hash"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        user.PasswordHash = "new-hash";
        dbContext.Entry(user).DetectChanges();

        var entry = dbContext.Entry(user);
        var changes = AuditDiff.CaptureChanges(entry);

        changes.Should().BeEmpty();
    }

    [Fact]
    public async Task CaptureChanges_UnchangedField_ExcludesIt()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Username = "jdoe",
            FullName = "SameName",
            Email = "john@example.com",
            PasswordHash = "hash"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        user.FullName = "SameName";
        dbContext.Entry(user).DetectChanges();

        var entry = dbContext.Entry(user);
        var changes = AuditDiff.CaptureChanges(entry);

        changes.Should().BeEmpty();
    }
}
