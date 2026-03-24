using Auth.Application;
using Auth.Application.Workspaces.Queries.GetWorkspaceById;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Workspaces.Queries.GetWorkspaceById;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Workspaces.Queries;

public sealed class GetWorkspaceByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingWorkspace_ReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var workspace = new Workspace { Name = "Test", Code = "test", Description = "desc" };
        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync();
        var handler = new GetWorkspaceByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetWorkspaceByIdQuery(workspace.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Code.Should().Be("test");
        result.Description.Should().Be("desc");
    }

    [Fact]
    public async Task Handle_NonExistent_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetWorkspaceByIdQueryHandler(dbContext);

        var result = await handler.Handle(new GetWorkspaceByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

}
