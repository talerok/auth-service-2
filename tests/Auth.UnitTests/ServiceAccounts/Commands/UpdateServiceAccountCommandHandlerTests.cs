using Auth.Application;
using Auth.Application.ServiceAccounts.Commands.UpdateServiceAccount;
using Auth.Infrastructure;
using Auth.Infrastructure.ServiceAccounts.Commands.UpdateServiceAccount;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.ServiceAccounts.Commands;

public sealed class UpdateServiceAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenServiceAccountExists_UpdatesAndReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "Old", Description = "Old desc", ClientId = "sa-123", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new UpdateServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdateServiceAccountCommand(serviceAccount.Id, "New", "New desc", false),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("New");
        result.Description.Should().Be("New desc");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenServiceAccountDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new UpdateServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new UpdateServiceAccountCommand(Guid.NewGuid(), "Name", "Desc", true),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SyncsOidcDisplayName()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount
        {
            Name = "SA", Description = "desc", ClientId = "sa-sync",
            IsActive = true
        };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();

        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var oidcApp = new object();
        appManager.Setup(x => x.FindByClientIdAsync("sa-sync", It.IsAny<CancellationToken>()))
            .ReturnsAsync(oidcApp);
        appManager.Setup(x => x.PopulateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), oidcApp, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.UpdateAsync(oidcApp, It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<object, OpenIddictApplicationDescriptor, CancellationToken>((_, d, _) => capturedDescriptor = d)
            .Returns(ValueTask.CompletedTask);

        var handler = new UpdateServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new UpdateServiceAccountCommand(serviceAccount.Id, "Updated SA", "desc", true),
            CancellationToken.None);

        capturedDescriptor.Should().NotBeNull();
        capturedDescriptor!.DisplayName.Should().Be("Updated SA");
    }

    [Fact]
    public async Task Handle_IndexesServiceAccountInSearch()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "SA", Description = "desc", ClientId = "sa-idx", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new UpdateServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new UpdateServiceAccountCommand(serviceAccount.Id, "Updated", "updated desc", true),
            CancellationToken.None);

        searchIndex.Verify(x => x.IndexServiceAccountAsync(
            It.Is<ServiceAccountDto>(d => d.Name == "Updated"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

}
