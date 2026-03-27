using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Auth.Application;
using Auth.Application.Oidc.Queries.ResolveAuthorizeRequest;
using Auth.Domain;
using Auth.Infrastructure.Oidc.Queries.ResolveAuthorizeRequest;
using FluentAssertions;
using Moq;
using OpenIddict.Abstractions;
using static Auth.UnitTests.TestDbContextFactory;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.UnitTests.Oidc;

public sealed class ResolveAuthorizeRequestQueryHandlerTests
{
    private const string ClientId = "test-client";
    private const string OidcAppId = "oidc-app-id";

    [Fact]
    public async Task Handle_AppRequiresEmailVerified_UserNotVerified_ReturnsEmailVerificationRequired()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Applications.Add(new Domain.Application
        {
            Name = "App", ClientId = ClientId, IsActive = true,
            RequireEmailVerified = true
        });
        dbContext.Users.Add(new User
        {
            Id = userId, Email = "test@test.com", PasswordHash = "hash",
            EmailVerified = false
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new ResolveAuthorizeRequestQuery(ClientId, userId, ["openid"]),
            CancellationToken.None);

        result.EmailVerificationRequired.Should().BeTrue();
        result.PhoneVerificationRequired.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AppRequiresEmailVerified_UserVerified_ReturnsNotRequired()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Applications.Add(new Domain.Application
        {
            Name = "App", ClientId = ClientId, IsActive = true,
            RequireEmailVerified = true
        });
        dbContext.Users.Add(new User
        {
            Id = userId, Email = "test@test.com", PasswordHash = "hash",
            EmailVerified = true
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new ResolveAuthorizeRequestQuery(ClientId, userId, ["openid"]),
            CancellationToken.None);

        result.EmailVerificationRequired.Should().BeFalse();
        result.PhoneVerificationRequired.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AppRequiresPhoneVerified_UserNotVerified_ReturnsPhoneVerificationRequired()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Applications.Add(new Domain.Application
        {
            Name = "App", ClientId = ClientId, IsActive = true,
            RequirePhoneVerified = true
        });
        dbContext.Users.Add(new User
        {
            Id = userId, Email = "test@test.com", PasswordHash = "hash",
            PhoneVerified = false
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new ResolveAuthorizeRequestQuery(ClientId, userId, ["openid"]),
            CancellationToken.None);

        result.EmailVerificationRequired.Should().BeFalse();
        result.PhoneVerificationRequired.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AppRequiresPhoneVerified_UserVerified_ReturnsNotRequired()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Applications.Add(new Domain.Application
        {
            Name = "App", ClientId = ClientId, IsActive = true,
            RequirePhoneVerified = true
        });
        dbContext.Users.Add(new User
        {
            Id = userId, Email = "test@test.com", PasswordHash = "hash",
            PhoneVerified = true
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new ResolveAuthorizeRequestQuery(ClientId, userId, ["openid"]),
            CancellationToken.None);

        result.EmailVerificationRequired.Should().BeFalse();
        result.PhoneVerificationRequired.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AppRequiresBothVerified_UserNeitherVerified_ReturnsBothRequired()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Applications.Add(new Domain.Application
        {
            Name = "App", ClientId = ClientId, IsActive = true,
            RequireEmailVerified = true,
            RequirePhoneVerified = true
        });
        dbContext.Users.Add(new User
        {
            Id = userId, Email = "test@test.com", PasswordHash = "hash",
            EmailVerified = false, PhoneVerified = false
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new ResolveAuthorizeRequestQuery(ClientId, userId, ["openid"]),
            CancellationToken.None);

        result.EmailVerificationRequired.Should().BeTrue();
        result.PhoneVerificationRequired.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AppNoVerificationRequired_ReturnsNoneRequired()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Applications.Add(new Domain.Application
        {
            Name = "App", ClientId = ClientId, IsActive = true,
            RequireEmailVerified = false,
            RequirePhoneVerified = false
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var result = await handler.Handle(
            new ResolveAuthorizeRequestQuery(ClientId, userId, ["openid"]),
            CancellationToken.None);

        result.EmailVerificationRequired.Should().BeFalse();
        result.PhoneVerificationRequired.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AppRequiresEmailVerified_UserNotFound_ThrowsAuthException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Applications.Add(new Domain.Application
        {
            Name = "App", ClientId = ClientId, IsActive = true,
            RequireEmailVerified = true
        });
        await dbContext.SaveChangesAsync();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new ResolveAuthorizeRequestQuery(ClientId, Guid.NewGuid(), ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(e => e.Code == AuthErrorCatalog.UserNotFound);
    }

    private static ResolveAuthorizeRequestQueryHandler CreateHandler(
        Infrastructure.AuthDbContext dbContext)
    {
        var appManager = new Mock<IOpenIddictApplicationManager>();
        var oidcApp = new object();
        appManager.Setup(x => x.FindByClientIdAsync(ClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oidcApp);
        appManager.Setup(x => x.GetIdAsync(oidcApp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OidcAppId);
        appManager.Setup(x => x.GetConsentTypeAsync(oidcApp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConsentTypes.Implicit);

        var authManager = new Mock<IOpenIddictAuthorizationManager>();
        authManager.Setup(x => x.FindAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<ImmutableArray<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(EmptyAsyncEnumerable());

        return new ResolveAuthorizeRequestQueryHandler(dbContext, appManager.Object, authManager.Object);
    }

    private static async IAsyncEnumerable<object> EmptyAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }
}
