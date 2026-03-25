using Auth.Application;
using Auth.Application.Workspaces.Commands.ImportWorkspaces;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Workspaces.Commands.ImportWorkspaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Workspaces.Commands;

public sealed class ImportWorkspacesCommandHandlerTests
{
    private static readonly Mock<IOpenIddictScopeManager> ScopeManager = new();
    [Fact]
    public async Task Import_CreatesNewWorkspaces()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportWorkspacesCommandHandler(dbContext, searchIndex.Object, ScopeManager.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportWorkspaceItem> { new("Dev", "dev", "Development") };
        var result = await handler.Handle(new ImportWorkspacesCommand(items), CancellationToken.None);

        result.Created.Should().Be(1);
        result.Updated.Should().Be(0);
        var ws = await dbContext.Workspaces.FirstAsync(w => w.Code == "dev");
        ws.Name.Should().Be("Dev");
        ws.IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task Import_UpdatesExistingWorkspaces()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Workspaces.Add(new Workspace { Name = "Old", Code = "dev", Description = "Old" });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportWorkspacesCommandHandler(dbContext, searchIndex.Object, ScopeManager.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportWorkspaceItem> { new("New", "dev", "Updated") };
        var result = await handler.Handle(new ImportWorkspacesCommand(items), CancellationToken.None);

        result.Created.Should().Be(0);
        result.Updated.Should().Be(1);
        var ws = await dbContext.Workspaces.FirstAsync(w => w.Code == "dev");
        ws.Name.Should().Be("New");
        ws.Description.Should().Be("Updated");
    }

    [Fact]
    public async Task Import_SystemWorkspace_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Workspaces.Add(new Workspace { Name = "System", Code = "system", Description = "System", IsSystem = true });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportWorkspacesCommandHandler(dbContext, searchIndex.Object, ScopeManager.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportWorkspaceItem> { new("Hacked", "system", "Hack") };
        var act = () => handler.Handle(new ImportWorkspacesCommand(items), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AuthException>();
        exception.Which.Code.Should().Be(AuthErrorCatalog.SystemWorkspaceImportForbidden);
    }

    [Fact]
    public async Task Import_MixedNewAndExisting_ReturnsCorrectCounts()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Workspaces.Add(new Workspace { Name = "Existing", Code = "existing", Description = "Old" });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportWorkspacesCommandHandler(dbContext, searchIndex.Object, ScopeManager.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportWorkspaceItem>
        {
            new("Updated", "existing", "Updated"),
            new("Brand New", "new-ws", "New workspace")
        };
        var result = await handler.Handle(new ImportWorkspacesCommand(items), CancellationToken.None);

        result.Created.Should().Be(1);
        result.Updated.Should().Be(1);
    }

    [Fact]
    public async Task Import_RestoresSoftDeletedWorkspace()
    {
        await using var dbContext = CreateDbContext();
        var deletedWs = new Workspace { Name = "Deleted", Code = "deleted", Description = "Old" };
        deletedWs.SoftDelete();
        dbContext.Workspaces.Add(deletedWs);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportWorkspacesCommandHandler(dbContext, searchIndex.Object, ScopeManager.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportWorkspaceItem> { new("Restored", "deleted", "Restored") };
        var result = await handler.Handle(new ImportWorkspacesCommand(items), CancellationToken.None);

        result.Updated.Should().Be(1);
        var ws = await dbContext.Workspaces.IgnoreQueryFilters().FirstAsync(w => w.Code == "deleted");
        ws.DeletedAt.Should().BeNull();
        ws.Name.Should().Be("Restored");
    }

    [Fact]
    public async Task Import_AddFalse_SkipsNewWorkspaces()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Workspaces.Add(new Workspace { Name = "Existing", Code = "existing", Description = "Old" });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportWorkspacesCommandHandler(dbContext, searchIndex.Object, ScopeManager.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportWorkspaceItem>
        {
            new("Updated", "existing", "Updated"),
            new("Brand New", "new-ws", "New workspace")
        };
        var result = await handler.Handle(new ImportWorkspacesCommand(items, Add: false), CancellationToken.None);

        result.Created.Should().Be(0);
        result.Updated.Should().Be(1);
        result.Skipped.Should().Be(1);
        (await dbContext.Workspaces.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Import_EditFalse_SkipsExistingWorkspaces()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Workspaces.Add(new Workspace { Name = "Existing", Code = "existing", Description = "Old" });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportWorkspacesCommandHandler(dbContext, searchIndex.Object, ScopeManager.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportWorkspaceItem>
        {
            new("Updated", "existing", "Updated"),
            new("Brand New", "new-ws", "New workspace")
        };
        var result = await handler.Handle(new ImportWorkspacesCommand(items, Edit: false), CancellationToken.None);

        result.Created.Should().Be(1);
        result.Updated.Should().Be(0);
        result.Skipped.Should().Be(1);
        var existing = await dbContext.Workspaces.FirstAsync(w => w.Code == "existing");
        existing.Name.Should().Be("Existing");
    }

    [Fact]
    public async Task Import_BothFalse_SkipsAll()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Workspaces.Add(new Workspace { Name = "Existing", Code = "existing", Description = "Old" });
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new ImportWorkspacesCommandHandler(dbContext, searchIndex.Object, ScopeManager.Object, new Mock<IAuditContext>().Object);

        var items = new List<ImportWorkspaceItem>
        {
            new("Updated", "existing", "Updated"),
            new("Brand New", "new-ws", "New workspace")
        };
        var result = await handler.Handle(new ImportWorkspacesCommand(items, Add: false, Edit: false), CancellationToken.None);

        result.Created.Should().Be(0);
        result.Updated.Should().Be(0);
        result.Skipped.Should().Be(2);
    }

}
