using System.Security.Claims;
using Auth.Application;
using Auth.Domain;
using Auth.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests;

public sealed class IdentitySourceAuthServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_WhenSourceNotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var act = () => service.AuthenticateAsync("nonexistent", "token", ["openid"], CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceNotFound);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenSourceDisabled_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateOidcSource("keycloak", isEnabled: false);
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var act = () => service.AuthenticateAsync("keycloak", "token", ["openid"], CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceDisabled);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenTypeMismatch_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "ldap-source",
            DisplayName = "LDAP",
            Type = IdentitySourceType.Ldap,
            IsEnabled = true
        };
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var act = () => service.AuthenticateAsync("ldap-source", "token", ["openid"], CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceTypeMismatch);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenLinkNotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateOidcSource("keycloak");
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var tokenValidator = new Mock<IOidcTokenValidator>();
        tokenValidator
            .Setup(x => x.ValidateAndGetSubjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("unknown-sub");

        var service = CreateService(dbContext, tokenValidator: tokenValidator);

        var act = () => service.AuthenticateAsync("keycloak", "token", ["openid"], CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceLinkNotFound);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserInactive_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateOidcSource("keycloak");
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            IsActive = false
        };
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
        {
            UserId = user.Id,
            IdentitySourceId = source.Id,
            ExternalIdentity = "ext-sub"
        });
        await dbContext.SaveChangesAsync();

        var tokenValidator = new Mock<IOidcTokenValidator>();
        tokenValidator
            .Setup(x => x.ValidateAndGetSubjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ext-sub");

        var service = CreateService(dbContext, tokenValidator: tokenValidator);

        var act = () => service.AuthenticateAsync("keycloak", "token", ["openid"], CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceUserInactive);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenValid_ReturnsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateOidcSource("keycloak");
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
        {
            UserId = user.Id,
            IdentitySourceId = source.Id,
            ExternalIdentity = "ext-sub"
        });
        await dbContext.SaveChangesAsync();

        var tokenValidator = new Mock<IOidcTokenValidator>();
        tokenValidator
            .Setup(x => x.ValidateAndGetSubjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ext-sub");

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", user.Id.ToString())]));
        var oidcGrantService = new Mock<IOidcGrantService>();
        oidcGrantService
            .Setup(x => x.BuildPrincipalAsync(user.Id, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        var service = CreateService(dbContext, tokenValidator: tokenValidator, oidcGrantService: oidcGrantService);

        var result = await service.AuthenticateAsync("keycloak", "token", ["openid"], CancellationToken.None);

        result.Should().BeOfType<PasswordGrantResult.Success>();
        var success = (PasswordGrantResult.Success)result;
        success.Principal.Should().Be(principal);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenMfaRequired_ReturnsMfaRequired()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateOidcSource("keycloak");
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        user.EnableTwoFactor(TwoFactorChannel.Email);
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
        {
            UserId = user.Id,
            IdentitySourceId = source.Id,
            ExternalIdentity = "ext-sub"
        });
        await dbContext.SaveChangesAsync();

        var tokenValidator = new Mock<IOidcTokenValidator>();
        tokenValidator
            .Setup(x => x.ValidateAndGetSubjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ext-sub");

        var challengeId = Guid.NewGuid();
        var authServiceMock = new Mock<IAuthService>();
        authServiceMock
            .Setup(x => x.CreateLoginChallengeAsync(user.Id, TwoFactorChannel.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorChallenge.Create(user.Id, TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email, "hash", "salt", "enc", DateTime.UtcNow.AddMinutes(5), 5));

        var service = CreateService(dbContext, authService: authServiceMock, tokenValidator: tokenValidator);

        var result = await service.AuthenticateAsync("keycloak", "token", ["openid"], CancellationToken.None);

        result.Should().BeOfType<PasswordGrantResult.MfaRequired>();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenPasswordChangeRequired_ReturnsPasswordChangeRequired()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateOidcSource("keycloak");
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        user.MarkMustChangePassword();
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
        {
            UserId = user.Id,
            IdentitySourceId = source.Id,
            ExternalIdentity = "ext-sub"
        });
        await dbContext.SaveChangesAsync();

        var tokenValidator = new Mock<IOidcTokenValidator>();
        tokenValidator
            .Setup(x => x.ValidateAndGetSubjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ext-sub");

        var challenge = PasswordChangeChallenge.Create(user.Id, DateTime.UtcNow.AddMinutes(15));
        var authServiceMock = new Mock<IAuthService>();
        authServiceMock
            .Setup(x => x.CreatePasswordChangeChallengeAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        var service = CreateService(dbContext, authService: authServiceMock, tokenValidator: tokenValidator);

        var result = await service.AuthenticateAsync("keycloak", "token", ["openid"], CancellationToken.None);

        result.Should().BeOfType<PasswordGrantResult.PasswordChangeRequired>();
    }

    private static IdentitySource CreateOidcSource(string name, bool isEnabled = true) => new()
    {
        Name = name,
        DisplayName = name,
        Type = IdentitySourceType.Oidc,
        IsEnabled = isEnabled,
        OidcConfig = new IdentitySourceOidcConfig
        {
            Authority = "https://idp.example.com",
            ClientId = "my-client"
        }
    };

    private static IdentitySourceAuthService CreateService(
        AuthDbContext dbContext,
        Mock<IAuthService>? authService = null,
        Mock<IOidcGrantService>? oidcGrantService = null,
        Mock<IOidcTokenValidator>? tokenValidator = null)
    {
        authService ??= new Mock<IAuthService>();
        oidcGrantService ??= new Mock<IOidcGrantService>();
        tokenValidator ??= new Mock<IOidcTokenValidator>();

        return new IdentitySourceAuthService(
            dbContext,
            authService.Object,
            oidcGrantService.Object,
            tokenValidator.Object);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuthDbContext(options);
    }
}
