using Auth.Application;
using Auth.Application.Oidc.Commands.HandleClientCredentialsGrant;
using Auth.Application.Oidc.Queries.GetApplicationAudiences;
using Auth.Application.Workspaces.Queries.BuildServiceAccountWorkspaceMasks;
using Auth.Infrastructure.Oidc.Commands.HandleClientCredentialsGrant;
using FluentAssertions;
using MediatR;
using Moq;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Oidc;

public sealed class HandleClientCredentialsGrantTests
{
    [Fact]
    public async Task HandleClientCredentialsGrant_WhenValid_ReturnsPrincipal()
    {
        var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount
        {
            Name = "Test Client",
            ClientId = "test-client",
            IsActive = true
        };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<GetApplicationAudiencesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        var handler = new HandleClientCredentialsGrantCommandHandler(sender.Object, dbContext);

        var principal = await handler.Handle(
            new HandleClientCredentialsGrantCommand("test-client", ["openid"]), CancellationToken.None);

        principal.FindFirst(Claims.Subject)!.Value.Should().Be(serviceAccount.Id.ToString());
        principal.FindFirst(Claims.Name)!.Value.Should().Be("Test Client");
    }

    [Fact]
    public async Task HandleClientCredentialsGrant_WhenClientNotFound_ThrowsAuthException()
    {
        var dbContext = CreateDbContext();
        var sender = new Mock<ISender>();
        var handler = new HandleClientCredentialsGrantCommandHandler(sender.Object, dbContext);

        var act = () => handler.Handle(
            new HandleClientCredentialsGrantCommand("nonexistent", ["openid"]), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.ApplicationNotFound);
    }

    [Fact]
    public async Task HandleClientCredentialsGrant_WhenClientInactive_ThrowsAuthException()
    {
        var dbContext = CreateDbContext();
        dbContext.ServiceAccounts.Add(new Domain.ServiceAccount
        {
            Name = "Inactive Client",
            ClientId = "inactive-client",
            IsActive = false
        });
        await dbContext.SaveChangesAsync();

        var sender = new Mock<ISender>();
        var handler = new HandleClientCredentialsGrantCommandHandler(sender.Object, dbContext);

        var act = () => handler.Handle(
            new HandleClientCredentialsGrantCommand("inactive-client", ["openid"]), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.ApplicationInactive);
    }

    [Fact]
    public async Task HandleClientCredentialsGrant_WhenWildcardWsScope_IncludesAllWorkspaces()
    {
        var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount
        {
            Name = "Wildcard Client",
            ClientId = "wildcard-client",
            IsActive = true
        };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<BuildServiceAccountWorkspaceMasksQuery>(q => q.ServiceAccountId == serviceAccount.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>>
            {
                ["system"] = new() { ["system"] = [0x01] },
                ["dev"] = new() { ["system"] = [0x02] }
            });
        sender.Setup(x => x.Send(It.IsAny<GetApplicationAudiencesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        var handler = new HandleClientCredentialsGrantCommandHandler(sender.Object, dbContext);

        var principal = await handler.Handle(
            new HandleClientCredentialsGrantCommand("wildcard-client", ["openid", "ws:*"]), CancellationToken.None);

        principal.FindFirst("ws:system").Should().NotBeNull();
        principal.FindFirst("ws:dev").Should().NotBeNull();
    }

    [Fact]
    public async Task HandleClientCredentialsGrant_WhenWsScope_IncludesWorkspaceMasks()
    {
        var dbContext = CreateDbContext();
        var serviceAccount = new Domain.ServiceAccount
        {
            Name = "WS Client",
            ClientId = "ws-client",
            IsActive = true
        };
        dbContext.ServiceAccounts.Add(serviceAccount);
        await dbContext.SaveChangesAsync();

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<BuildServiceAccountWorkspaceMasksQuery>(q => q.ServiceAccountId == serviceAccount.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>> { ["system"] = new() { ["system"] = [0b_0000_0011] } });
        sender.Setup(x => x.Send(It.IsAny<GetApplicationAudiencesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        var handler = new HandleClientCredentialsGrantCommandHandler(sender.Object, dbContext);

        var principal = await handler.Handle(
            new HandleClientCredentialsGrantCommand("ws-client", ["openid", "ws:system"]), CancellationToken.None);

        var wsClaim = principal.FindFirst("ws:system");
        wsClaim.Should().NotBeNull();
        wsClaim!.Value.Should().Contain("system");
    }
}
