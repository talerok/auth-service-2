using Auth.Application;
using Auth.Application.Workspaces.Queries.ExportWorkspaces;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Workspaces.Queries.ExportWorkspaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Workspaces.Queries;

public sealed class ExportWorkspacesQueryHandlerTests
{
    [Fact]
    public async Task Export_ReturnsOnlyNonSystemWorkspaces()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Workspaces.AddRange(
            new Workspace { Name = "System", Code = "system", Description = "System", IsSystem = true },
            new Workspace { Name = "Dev", Code = "dev", Description = "Development" });
        await dbContext.SaveChangesAsync();
        var handler = new ExportWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(new ExportWorkspacesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Code.Should().Be("dev");
    }

    [Fact]
    public async Task Export_ExcludesSoftDeleted()
    {
        await using var dbContext = CreateDbContext();
        var deleted = new Workspace { Name = "Deleted", Code = "deleted", Description = "Deleted" };
        deleted.SoftDelete();
        dbContext.Workspaces.AddRange(
            new Workspace { Name = "Active", Code = "active", Description = "Active" },
            deleted);
        await dbContext.SaveChangesAsync();
        var handler = new ExportWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(new ExportWorkspacesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Code.Should().Be("active");
    }

    [Fact]
    public async Task Export_OrdersByCode()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Workspaces.AddRange(
            new Workspace { Name = "Z", Code = "z-ws", Description = "Z" },
            new Workspace { Name = "A", Code = "a-ws", Description = "A" });
        await dbContext.SaveChangesAsync();
        var handler = new ExportWorkspacesQueryHandler(dbContext);

        var result = await handler.Handle(new ExportWorkspacesQuery(), CancellationToken.None);

        result.Select(x => x.Code).Should().BeInAscendingOrder();
    }

}
