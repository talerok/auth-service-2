using Auth.Application;
using Auth.Application.ServiceAccounts.Commands.PatchServiceAccount;
using Auth.Infrastructure;
using Auth.Infrastructure.ServiceAccounts.Commands.PatchServiceAccount;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.ServiceAccounts.Commands;

public sealed class PatchServiceAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenServiceAccountExists_PatchesOnlyProvidedFields()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "Original", Description = "Orig desc", ClientId = "sa-1", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new PatchServiceAccountCommand(serviceAccount.Id, "Patched", null, null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Patched");
        result.Description.Should().Be("Orig desc");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenServiceAccountDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new PatchServiceAccountCommand(Guid.NewGuid(), "Name", null, null),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenNamePatched_UpdatesOidcDisplayName()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "Old", Description = "desc", ClientId = "sa-2", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        appManager.Setup(x => x.FindByClientIdAsync("sa-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
        appManager.Setup(x => x.PopulateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var handler = new PatchServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new PatchServiceAccountCommand(serviceAccount.Id, "New Name", null, null),
            CancellationToken.None);

        appManager.Verify(x => x.UpdateAsync(It.IsAny<object>(), It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNameNotPatched_DoesNotTouchOidc()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "Name", Description = "desc", ClientId = "sa-3", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new PatchServiceAccountCommand(serviceAccount.Id, null, "new desc", null),
            CancellationToken.None);

        appManager.Verify(x => x.FindByClientIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PatchesIsActive()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "SA", Description = "desc", ClientId = "sa-4", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        var result = await handler.Handle(
            new PatchServiceAccountCommand(serviceAccount.Id, null, null, false),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_IndexesServiceAccountInSearch()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "SA", Description = "desc", ClientId = "sa-5", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new PatchServiceAccountCommandHandler(dbContext, searchIndex.Object, appManager.Object);

        await handler.Handle(
            new PatchServiceAccountCommand(serviceAccount.Id, null, "new desc", null),
            CancellationToken.None);

        searchIndex.Verify(x => x.IndexServiceAccountAsync(
            It.Is<ServiceAccountDto>(d => d.Description == "new desc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

}
