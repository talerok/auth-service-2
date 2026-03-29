using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Application.ServiceAccounts.Commands.CreateServiceAccount;
using Auth.Infrastructure;
using Auth.Infrastructure.ServiceAccounts.Commands.CreateServiceAccount;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using OidcConstants = OpenIddict.Abstractions.OpenIddictConstants;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.ServiceAccounts.Commands;

public sealed class CreateServiceAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesServiceAccountAndReturnsResponse()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new CreateServiceAccountCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new CreateServiceAccountCommand("My SA", "Some description", true),
            CancellationToken.None);

        result.ServiceAccount.Name.Should().Be("My SA");
        result.ServiceAccount.Description.Should().Be("Some description");
        result.ServiceAccount.IsActive.Should().BeTrue();
        result.ServiceAccount.ClientId.Should().StartWith("sa-");
        result.ClientSecret.Should().NotBeNullOrEmpty();

        var saved = await dbContext.ServiceAccounts.FirstAsync();
        saved.Name.Should().Be("My SA");
    }

    [Fact]
    public async Task Handle_RegistersOpenIddictApplication()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new CreateServiceAccountCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateServiceAccountCommand("My SA", "desc"),
            CancellationToken.None);

        appManager.Verify(x => x.CreateAsync(
            It.Is<OpenIddictApplicationDescriptor>(d => d.DisplayName == "My SA"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IndexesServiceAccountInSearch()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new CreateServiceAccountCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateServiceAccountCommand("My SA", "desc"),
            CancellationToken.None);

        eventBus.Verify(x => x.PublishAsync(
            It.Is<IndexEntityRequested>(e => e.EntityType == IndexEntityType.ServiceAccount && e.Operation == IndexOperation.Index),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SetsClientCredentialsPermissions()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => capturedDescriptor = d)
            .ReturnsAsync(new object());
        var handler = new CreateServiceAccountCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        await handler.Handle(
            new CreateServiceAccountCommand("SA", "desc"),
            CancellationToken.None);

        capturedDescriptor.Should().NotBeNull();
        capturedDescriptor!.Permissions.Should().Contain(OidcConstants.Permissions.GrantTypes.ClientCredentials);
        capturedDescriptor.Permissions.Should().Contain(OidcConstants.Permissions.Endpoints.Token);
        capturedDescriptor.Permissions.Should().Contain(OidcConstants.Permissions.Prefixes.Scope + "ws:*");
        capturedDescriptor.Permissions.Should().NotContain(OidcConstants.Permissions.Scopes.Email);
        capturedDescriptor.Permissions.Should().NotContain(OidcConstants.Permissions.Scopes.Profile);
        capturedDescriptor.ClientType.Should().Be(OidcConstants.ClientTypes.Confidential);
    }

    [Fact]
    public async Task Handle_AlwaysReturnsSecret()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new CreateServiceAccountCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new CreateServiceAccountCommand("SA", "desc"),
            CancellationToken.None);

        result.ClientSecret.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_DefaultIsActive_True()
    {
        await using var dbContext = CreateDbContext();
        var eventBus = new Mock<IEventBus>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new CreateServiceAccountCommandHandler(dbContext, eventBus.Object, appManager.Object, new Mock<IAuditContext>().Object);

        var result = await handler.Handle(
            new CreateServiceAccountCommand("SA", "desc"),
            CancellationToken.None);

        result.ServiceAccount.IsActive.Should().BeTrue();
    }

}
