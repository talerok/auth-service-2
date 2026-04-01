using Auth.Application;
using Auth.Application.Auth.Queries.GetActiveUser;
using OpenIddict.Abstractions;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using Auth.Application.Oidc.Queries.GetApplicationAudiences;
using Auth.Application.Workspaces.Queries.BuildWorkspaceMasks;
using Auth.Domain;
using Auth.Infrastructure.Oidc.Queries.BuildPrincipal;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static Auth.UnitTests.TestDbContextFactory;
using static Auth.UnitTests.Oidc.OidcTestHelpers;

namespace Auth.UnitTests.Oidc;

public sealed class BuildPrincipalQueryHandlerTests
{
    [Fact]
    public async Task BuildPrincipal_SetsCoreClaimsAndDestinations()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"]), CancellationToken.None);

        principal.FindFirst(Claims.Subject)!.Value.Should().Be(TestUser.Id.ToString());
        principal.FindFirst(Claims.Name)!.Value.Should().Be("Test User");
        principal.FindFirst(Claims.PreferredUsername)!.Value.Should().Be("testuser");
        principal.FindFirst(Claims.Email).Should().BeNull("email scope was not requested");
        principal.FindFirst(Claims.AuthenticationTime).Should().NotBeNull();
    }

    [Fact]
    public async Task BuildPrincipal_WhenEmailScope_IncludesEmailClaim()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "email"]), CancellationToken.None);

        principal.FindFirst(Claims.Email)!.Value.Should().Be("test@example.com");
    }

    [Fact]
    public async Task BuildPrincipal_WhenPhoneScope_IncludesPhoneClaim()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "phone"]), CancellationToken.None);

        principal.FindFirst(Claims.PhoneNumber)!.Value.Should().Be("+1234567890");
    }

    [Fact]
    public async Task BuildPrincipal_WhenPhoneScopeButNoPhone_OmitsPhoneClaim()
    {
        var userNoPhone = new User
        {
            Id = Guid.NewGuid(), Username = "nophone", FullName = "No Phone",
            Email = "nophone@test.com", PasswordHash = "hash", IsActive = true
        };
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == userNoPhone.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userNoPhone);
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(userNoPhone.Id, ["openid", "phone"]), CancellationToken.None);

        principal.FindFirst(Claims.PhoneNumber).Should().BeNull();
    }

    [Fact]
    public async Task BuildPrincipal_WhenWsScope_IncludesWorkspaceMasks()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(
                It.Is<BuildWorkspaceMasksQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>> { ["system"] = new() { ["system"] = [0b_0000_0101] } });
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "ws:system"]), CancellationToken.None);

        var wsClaim = principal.FindFirst("ws:system");
        wsClaim.Should().NotBeNull();
        wsClaim!.Value.Should().Contain("system");
        wsClaim.Value.Should().Contain(Convert.ToBase64String([0b_0000_0101]));
    }

    [Fact]
    public async Task BuildPrincipal_WhenMultipleWsScopes_IncludesOnlyRequested()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(
                It.Is<BuildWorkspaceMasksQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>>
            {
                ["system"] = new() { ["system"] = [0x01] },
                ["dev"] = new() { ["system"] = [0x02] },
                ["other"] = new() { ["system"] = [0x04] }
            });
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "ws:system", "ws:dev"]), CancellationToken.None);

        principal.FindFirst("ws:system").Should().NotBeNull();
        principal.FindFirst("ws:dev").Should().NotBeNull();
        principal.FindFirst("ws:other").Should().BeNull();
    }

    [Fact]
    public async Task BuildPrincipal_WhenWsScopeForInaccessibleWorkspace_OmitsFromClaim()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(
                It.Is<BuildWorkspaceMasksQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>>());
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "ws:unknown"]), CancellationToken.None);

        principal.FindFirst("ws:unknown").Should().BeNull();
    }

    [Fact]
    public async Task BuildPrincipal_WhenNoWsScope_DoesNotIncludeWsClaim()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"]), CancellationToken.None);

        principal.Claims.Should().NotContain(c => c.Type.StartsWith("ws:"));
        sender.Verify(x => x.Send(It.IsAny<BuildWorkspaceMasksQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuildPrincipal_SetsCorrectDestinations()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile", "email"]), CancellationToken.None);

        var subClaim = principal.FindFirst(Claims.Subject)!;
        subClaim.GetDestinations().Should().Contain(Destinations.AccessToken);
        subClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);

        var nameClaim = principal.FindFirst(Claims.Name)!;
        nameClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);
        nameClaim.GetDestinations().Should().Contain(Destinations.AccessToken);

        var authTimeClaim = principal.FindFirst(Claims.AuthenticationTime)!;
        authTimeClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);
        authTimeClaim.GetDestinations().Should().NotContain(Destinations.AccessToken);
    }

    [Fact]
    public async Task BuildPrincipal_WhenAuthMethodsProvided_IncludesAmrClaims()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"], AuthMethods: ["pwd", "otp"]), CancellationToken.None);

        var amrClaims = principal.FindAll(Claims.AuthenticationMethodReference).Select(c => c.Value).ToList();
        amrClaims.Should().BeEquivalentTo(["pwd", "otp"]);
    }

    [Fact]
    public async Task BuildPrincipal_WhenNoAuthMethods_OmitsAmrClaims()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"]), CancellationToken.None);

        principal.FindAll(Claims.AuthenticationMethodReference).Should().BeEmpty();
    }

    [Fact]
    public async Task BuildPrincipal_AmrClaimsDestination_IsIdentityToken()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"], AuthMethods: ["pwd"]), CancellationToken.None);

        var amrClaim = principal.FindFirst(Claims.AuthenticationMethodReference)!;
        amrClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);
        amrClaim.GetDestinations().Should().NotContain(Destinations.AccessToken);
    }

    [Fact]
    public async Task BuildPrincipal_WhenExpirationActive_IncludesPwdExpClaim()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Username = "expuser", FullName = "Exp User",
            Email = "exp@test.com", IsActive = true
        };
        user.SetPassword("hash");

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == user.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions { DefaultMaxAgeDays = 90 }));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(user.Id, ["openid", "profile"]), CancellationToken.None);

        var pwdExpClaim = principal.FindFirst("pwd_exp");
        pwdExpClaim.Should().NotBeNull();
        var expected = new DateTimeOffset(user.PasswordChangedAt!.Value.AddDays(90), TimeSpan.Zero).ToUnixTimeSeconds();
        long.Parse(pwdExpClaim!.Value).Should().Be(expected);
    }

    [Fact]
    public async Task BuildPrincipal_WhenExpirationDisabled_NoPwdExpClaim()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions { DefaultMaxAgeDays = 0 }));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "profile"]), CancellationToken.None);

        principal.FindFirst("pwd_exp").Should().BeNull();
    }

    [Fact]
    public async Task BuildPrincipal_PwdExpClaim_GoesToBothTokens()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Username = "destuser", FullName = "Dest User",
            Email = "dest@test.com", IsActive = true
        };
        user.SetPassword("hash");

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == user.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions { DefaultMaxAgeDays = 90 }));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(user.Id, ["openid", "profile"]), CancellationToken.None);

        var pwdExpClaim = principal.FindFirst("pwd_exp")!;
        pwdExpClaim.GetDestinations().Should().Contain(Destinations.AccessToken);
        pwdExpClaim.GetDestinations().Should().Contain(Destinations.IdentityToken);
    }

    [Fact]
    public async Task BuildPrincipal_WhenWildcardWsScope_IncludesAllAccessibleWorkspaces()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(
                It.Is<BuildWorkspaceMasksQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>>
            {
                ["system"] = new() { ["system"] = [0x01] },
                ["dev"] = new() { ["system"] = [0x02] },
                ["staging"] = new() { ["system"] = [0x04] }
            });
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "ws:*"]), CancellationToken.None);

        principal.FindFirst("ws:system").Should().NotBeNull();
        principal.FindFirst("ws:dev").Should().NotBeNull();
        principal.FindFirst("ws:staging").Should().NotBeNull();
    }

    [Fact]
    public async Task BuildPrincipal_WhenWildcardWsScopeButNoAccess_NoClaims()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(
                It.Is<BuildWorkspaceMasksQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>>());
        var handler = new BuildPrincipalQueryHandler(sender.Object, CreateDbContext(), Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "ws:*"]), CancellationToken.None);

        principal.Claims.Should().NotContain(c => c.Type.StartsWith("ws:"));
    }

    // Scope filtering tests

    [Fact]
    public async Task BuildPrincipal_WhenClientId_FiltersScopesToApplicationAllowed()
    {
        var dbContext = CreateDbContext();
        var app = new Domain.Application
        {
            Name = "Test App", ClientId = "test-app", IsActive = true
        };
        app.SetScopes(["email", "profile"]);
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<GetActiveUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(It.IsAny<GetApplicationAudiencesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        var handler = new BuildPrincipalQueryHandler(sender.Object, dbContext, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "email", "phone"], ClientId: "test-app"), CancellationToken.None);

        principal.FindFirst(Claims.Email)!.Value.Should().Be("test@example.com");
        principal.FindFirst(Claims.PhoneNumber).Should().BeNull("phone is not in application's allowed scopes");
    }

    [Fact]
    public async Task BuildPrincipal_WhenClientId_PassthroughScopesAlwaysPresent()
    {
        var dbContext = CreateDbContext();
        var app = new Domain.Application
        {
            Name = "Minimal App", ClientId = "minimal-app", IsActive = true
        };
        app.SetScopes(["profile"]);
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<GetActiveUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(It.IsAny<GetApplicationAudiencesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        var handler = new BuildPrincipalQueryHandler(sender.Object, dbContext, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "offline_access", "profile", "email"], ClientId: "minimal-app"), CancellationToken.None);

        principal.FindFirst(Claims.Subject).Should().NotBeNull("openid is passthrough");
        principal.FindFirst(Claims.Name)!.Value.Should().Be("Test User", "profile is allowed");
        principal.FindFirst(Claims.Email).Should().BeNull("email is not in application's allowed scopes");
    }

    [Fact]
    public async Task BuildPrincipal_WhenClientIdWithWsWildcard_AllowsWorkspaceScopes()
    {
        var dbContext = CreateDbContext();
        var app = new Domain.Application
        {
            Name = "WS App", ClientId = "ws-app", IsActive = true
        };
        app.SetScopes(["email", "ws:*"]);
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(
                It.Is<BuildWorkspaceMasksQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Dictionary<string, byte[]>>
            {
                ["finance"] = new() { ["system"] = [0x01] }
            });
        sender.Setup(x => x.Send(It.IsAny<GetApplicationAudiencesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        var handler = new BuildPrincipalQueryHandler(sender.Object, dbContext, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "email", "phone", "ws:finance"], ClientId: "ws-app"), CancellationToken.None);

        principal.FindFirst(Claims.Email).Should().NotBeNull("email is allowed");
        principal.FindFirst(Claims.PhoneNumber).Should().BeNull("phone is not allowed");
        principal.FindFirst("ws:finance").Should().NotBeNull("ws:* allows any ws: scope");
    }

    [Fact]
    public async Task BuildPrincipal_WhenApplicationNotFound_ReturnsScopesAsIs()
    {
        var dbContext = CreateDbContext();
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                It.Is<GetActiveUserQuery>(q => q.UserId == TestUser.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser);
        sender.Setup(x => x.Send(It.IsAny<GetApplicationAudiencesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        var handler = new BuildPrincipalQueryHandler(sender.Object, dbContext, Options.Create(new PasswordExpirationOptions()));

        var principal = await handler.Handle(
            new BuildPrincipalQuery(TestUser.Id, ["openid", "email", "phone"], ClientId: "nonexistent"), CancellationToken.None);

        principal.FindFirst(Claims.Email).Should().NotBeNull("no app found — scopes pass through");
        principal.FindFirst(Claims.PhoneNumber).Should().NotBeNull("no app found — scopes pass through");
    }
}
