using Auth.Application.ServiceAccounts.Commands.RegenerateServiceAccountSecret;
using Auth.Infrastructure;
using Auth.Infrastructure.ServiceAccounts.Commands.RegenerateServiceAccountSecret;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.ServiceAccounts.Commands;

public sealed class RegenerateServiceAccountSecretCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenServiceAccountExists_ReturnsNewSecret()
    {
        await using var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount { Name = "SA", Description = "desc", ClientId = "sa-regen", IsActive = true };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var oidcApp = new object();
        appManager.Setup(x => x.FindByClientIdAsync("sa-regen", It.IsAny<CancellationToken>()))
            .ReturnsAsync(oidcApp);
        appManager.Setup(x => x.PopulateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), oidcApp, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var handler = new RegenerateServiceAccountSecretCommandHandler(dbContext, appManager.Object);

        var result = await handler.Handle(
            new RegenerateServiceAccountSecretCommand(serviceAccount.Id),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ClientSecret.Should().NotBeNullOrEmpty();
        appManager.Verify(x => x.UpdateAsync(oidcApp, It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceAccountDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new RegenerateServiceAccountSecretCommandHandler(dbContext, appManager.Object);

        var result = await handler.Handle(
            new RegenerateServiceAccountSecretCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeNull();
    }

}
