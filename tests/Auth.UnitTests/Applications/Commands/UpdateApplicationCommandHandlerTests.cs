using Auth.Application;
using Auth.Application.Applications.Commands.UpdateApplication;
using Auth.Infrastructure;
using Auth.Infrastructure.Applications.Commands.UpdateApplication;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using OidcConstants = OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.UnitTests.Applications.Commands;

public sealed class UpdateApplicationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenApplicationExists_UpdatesAndReturnsDto()
    {
        await using var dbContext = CreateDbContext();
        var application = new Domain.Application { Name = "Old", Description = "Old desc", ClientId = "ac-123", IsActive = true };
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var corsOriginService = new Mock<ICorsOriginService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new UpdateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object);

        var result = await handler.Handle(
            new UpdateApplicationCommand(application.Id, "New", "New desc", false,
                null, null, [], [], [], null, [], ["authorization_code", "refresh_token"], null, null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("New");
        result.Description.Should().Be("New desc");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenApplicationDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var searchIndex = new Mock<ISearchIndexService>();
        var corsOriginService = new Mock<ICorsOriginService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new UpdateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object);

        var result = await handler.Handle(
            new UpdateApplicationCommand(Guid.NewGuid(), "Name", "Desc", true,
                null, null, [], [], [], null, [], ["authorization_code", "refresh_token"], null, null),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UpdatesOAuthFields()
    {
        await using var dbContext = CreateDbContext();
        var application = new Domain.Application { Name = "App", Description = "desc", ClientId = "ac-oauth", IsActive = true };
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync();
        var searchIndex = new Mock<ISearchIndexService>();
        var corsOriginService = new Mock<ICorsOriginService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var handler = new UpdateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object);

        var result = await handler.Handle(
            new UpdateApplicationCommand(application.Id, "OAuth App", "updated", true,
                "https://example.com/logo.png", "https://example.com",
                ["https://example.com/cb"], ["https://example.com/logout"], [], "implicit", [], ["authorization_code", "refresh_token"], null, null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.LogoUrl.Should().Be("https://example.com/logo.png");
        result.HomepageUrl.Should().Be("https://example.com");
        result.RedirectUris.Should().ContainSingle("https://example.com/cb");
        result.PostLogoutRedirectUris.Should().ContainSingle("https://example.com/logout");
    }

    [Fact]
    public async Task Handle_SyncsOidcApp_WithOAuthPermissions()
    {
        await using var dbContext = CreateDbContext();
        var application = new Domain.Application
        {
            Name = "App", Description = "desc", ClientId = "ac-sync",
            IsActive = true
        };
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync();

        var searchIndex = new Mock<ISearchIndexService>();
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var oidcApp = new object();
        appManager.Setup(x => x.FindByClientIdAsync("ac-sync", It.IsAny<CancellationToken>()))
            .ReturnsAsync(oidcApp);
        appManager.Setup(x => x.PopulateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), oidcApp, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        OpenIddictApplicationDescriptor? capturedDescriptor = null;
        appManager.Setup(x => x.UpdateAsync(oidcApp, It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<object, OpenIddictApplicationDescriptor, CancellationToken>((_, d, _) => capturedDescriptor = d)
            .Returns(ValueTask.CompletedTask);

        var corsOriginService = new Mock<ICorsOriginService>();
        var handler = new UpdateApplicationCommandHandler(dbContext, searchIndex.Object, corsOriginService.Object, appManager.Object);

        await handler.Handle(
            new UpdateApplicationCommand(application.Id, "OAuth App", "desc", true,
                null, null,
                ["https://example.com/cb"], [], [], "explicit", ["email", "profile"], ["authorization_code", "refresh_token"], null, null),
            CancellationToken.None);

        capturedDescriptor.Should().NotBeNull();
        capturedDescriptor!.Permissions.Should().Contain(OidcConstants.Permissions.GrantTypes.AuthorizationCode);
        capturedDescriptor.Permissions.Should().Contain(OidcConstants.Permissions.Endpoints.Authorization);
        capturedDescriptor.Requirements.Should().Contain(OidcConstants.Requirements.Features.ProofKeyForCodeExchange);
        capturedDescriptor.ConsentType.Should().Be(OidcConstants.ConsentTypes.Explicit);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
