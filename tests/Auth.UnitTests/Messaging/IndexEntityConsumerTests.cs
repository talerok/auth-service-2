using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Messaging.Consumers;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Messaging;

public sealed class IndexEntityConsumerTests
{
    private readonly Mock<ISearchIndexService> _searchIndex = new();

    private IndexEntityConsumer CreateConsumer(AuthDbContext dbContext) =>
        new(dbContext, _searchIndex.Object, NullLogger<IndexEntityConsumer>.Instance);

    private static Mock<ConsumeContext<IndexEntityRequested>> CreateContext(
        IndexEntityType entityType, Guid entityId, IndexOperation operation = IndexOperation.Index)
    {
        var context = new Mock<ConsumeContext<IndexEntityRequested>>();
        context.Setup(x => x.Message).Returns(new IndexEntityRequested
        {
            EntityType = entityType, EntityId = entityId, Operation = operation
        });
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Consume_IndexUser_CallsSearchIndex()
    {
        await using var dbContext = CreateDbContext();
        var user = new User { Username = "alice", Email = "alice@test.com", FullName = "Alice", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(IndexEntityType.User, user.Id);

        await consumer.Consume(context.Object);

        _searchIndex.Verify(x => x.IndexUserAsync(
            It.Is<UserDto>(dto => dto.Id == user.Id && dto.Username == "alice"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_DeleteUser_CallsDeleteOnSearchIndex()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();

        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(IndexEntityType.User, userId, IndexOperation.Delete);

        await consumer.Consume(context.Object);

        _searchIndex.Verify(x => x.DeleteUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_EntityNotFound_SkipsIndexing()
    {
        await using var dbContext = CreateDbContext();
        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(IndexEntityType.Role, Guid.NewGuid());

        await consumer.Consume(context.Object);

        _searchIndex.Verify(x => x.IndexRoleAsync(It.IsAny<RoleDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_IndexPermission_CallsSearchIndex()
    {
        await using var dbContext = CreateDbContext();
        var permission = new Permission { Domain = "users", Code = "read", Bit = 1, Description = "Read users" };
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();

        var consumer = CreateConsumer(dbContext);
        var context = CreateContext(IndexEntityType.Permission, permission.Id);

        await consumer.Consume(context.Object);

        _searchIndex.Verify(x => x.IndexPermissionAsync(
            It.Is<PermissionDto>(dto => dto.Id == permission.Id && dto.Code == "read"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
