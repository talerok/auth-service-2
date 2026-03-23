using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Application.Oidc.Commands.HandleJwtBearerGrant;
using Auth.Application.Oidc.Commands.HandleLdapGrant;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Oidc.Commands.HandleJwtBearerGrant;
using Auth.Infrastructure.Oidc.Commands.HandleLdapGrant;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace Auth.UnitTests;

public sealed class JwtBearerGrantHandlerTests
{
    private const string Issuer = "https://idp.example.com";
    private const string ClientId = "my-client";

    [Fact]
    public async Task Handle_WhenAssertionMissing_ThrowsInvalidRequest()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new HandleJwtBearerGrantCommand(null, ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidRequest);
    }

    [Fact]
    public async Task Handle_WhenAssertionIsNotValidJwt_ThrowsTokenInvalid()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new HandleJwtBearerGrantCommand("not-a-jwt", ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceTokenInvalid);
    }

    [Fact]
    public async Task Handle_WhenIssuerNotFound_ThrowsSourceNotFound()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var jwt = CreateFakeJwt("https://unknown-issuer.example.com");

        var act = () => handler.Handle(
            new HandleJwtBearerGrantCommand(jwt, ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceNotFound);
    }

    [Fact]
    public async Task Handle_WhenSourceDisabled_ThrowsDisabled()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateOidcSource("keycloak", isEnabled: false);
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext);
        var jwt = CreateFakeJwt(Issuer);

        var act = () => handler.Handle(
            new HandleJwtBearerGrantCommand(jwt, ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceDisabled);
    }

    [Fact]
    public async Task Handle_WhenLinkNotFound_ThrowsLinkNotFound()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateOidcSource("keycloak");
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var tokenValidator = new Mock<IOidcTokenValidator>();
        tokenValidator
            .Setup(x => x.ValidateAndGetSubjectAsync(Issuer, ClientId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("unknown-sub");

        var handler = CreateHandler(dbContext, tokenValidator: tokenValidator);
        var jwt = CreateFakeJwt(Issuer);

        var act = () => handler.Handle(
            new HandleJwtBearerGrantCommand(jwt, ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceLinkNotFound);
    }

    [Fact]
    public async Task Handle_WhenUserInactive_ThrowsUserInactive()
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
            .Setup(x => x.ValidateAndGetSubjectAsync(Issuer, ClientId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ext-sub");

        var handler = CreateHandler(dbContext, tokenValidator: tokenValidator);
        var jwt = CreateFakeJwt(Issuer);

        var act = () => handler.Handle(
            new HandleJwtBearerGrantCommand(jwt, ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceUserInactive);
    }

    [Fact]
    public async Task Handle_WhenValid_ReturnsSuccess()
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
            .Setup(x => x.ValidateAndGetSubjectAsync(Issuer, ClientId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ext-sub");

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", user.Id.ToString())]));
        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(
                It.Is<BuildPrincipalQuery>(q => q.UserId == user.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        var handler = CreateHandler(dbContext, sender: senderMock, tokenValidator: tokenValidator);
        var jwt = CreateFakeJwt(Issuer);

        var result = await handler.Handle(
            new HandleJwtBearerGrantCommand(jwt, ["openid"]),
            CancellationToken.None);

        result.Should().BeOfType<CredentialValidationResult.Success>();
        var success = (CredentialValidationResult.Success)result;
        success.Principal.Should().Be(principal);
    }

    [Fact]
    public async Task Handle_WhenMfaRequired_ReturnsMfaRequired()
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
            .Setup(x => x.ValidateAndGetSubjectAsync(Issuer, ClientId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ext-sub");

        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(
                It.Is<CreateLoginChallengeCommand>(c => c.UserId == user.Id && c.Channel == TwoFactorChannel.Email),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorChallenge.Create(user.Id, TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email, "hash", "salt", "enc", DateTime.UtcNow.AddMinutes(5), 5));

        var handler = CreateHandler(dbContext, sender: senderMock, tokenValidator: tokenValidator);
        var jwt = CreateFakeJwt(Issuer);

        var result = await handler.Handle(
            new HandleJwtBearerGrantCommand(jwt, ["openid"]),
            CancellationToken.None);

        result.Should().BeOfType<CredentialValidationResult.MfaRequired>();
    }

    [Fact]
    public async Task Handle_WhenPasswordChangeRequired_IgnoresAndReturnsSuccess()
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
            .Setup(x => x.ValidateAndGetSubjectAsync(Issuer, ClientId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ext-sub");

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", user.Id.ToString())]));
        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(
                It.Is<BuildPrincipalQuery>(q => q.UserId == user.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        var handler = CreateHandler(dbContext, sender: senderMock, tokenValidator: tokenValidator);
        var jwt = CreateFakeJwt(Issuer);

        var result = await handler.Handle(
            new HandleJwtBearerGrantCommand(jwt, ["openid"]),
            CancellationToken.None);

        result.Should().BeOfType<CredentialValidationResult.Success>();
    }

    private static IdentitySource CreateOidcSource(string name, bool isEnabled = true) => new()
    {
        Name = name,
        Code = name,
        DisplayName = name,
        Type = IdentitySourceType.Oidc,
        IsEnabled = isEnabled,
        OidcConfig = new IdentitySourceOidcConfig
        {
            Authority = Issuer,
            ClientId = ClientId
        }
    };

    private static HandleJwtBearerGrantCommandHandler CreateHandler(
        AuthDbContext dbContext,
        Mock<ISender>? sender = null,
        Mock<IOidcTokenValidator>? tokenValidator = null)
    {
        sender ??= new Mock<ISender>();
        tokenValidator ??= new Mock<IOidcTokenValidator>();

        return new HandleJwtBearerGrantCommandHandler(
            dbContext,
            sender.Object,
            tokenValidator.Object);
    }

    private static string CreateFakeJwt(string issuer)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(
            issuer: issuer,
            audience: "test",
            subject: new ClaimsIdentity([new Claim("sub", "ext-sub")]),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(new byte[32]),
                SecurityAlgorithms.HmacSha256));
        return handler.WriteToken(token);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuthDbContext(options);
    }
}

public sealed class LdapGrantHandlerTests
{
    [Fact]
    public async Task Handle_WhenIdentitySourceMissing_ThrowsInvalidRequest()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new HandleLdapGrantCommand(null, "jdoe", "password", ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidRequest);
    }

    [Fact]
    public async Task Handle_WhenUsernameMissing_ThrowsInvalidRequest()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new HandleLdapGrantCommand("corporate-ldap", null, "password", ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidRequest);
    }

    [Fact]
    public async Task Handle_WhenPasswordMissing_ThrowsInvalidRequest()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new HandleLdapGrantCommand("corporate-ldap", "jdoe", null, ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.InvalidRequest);
    }

    [Fact]
    public async Task Handle_WhenSourceNotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new HandleLdapGrantCommand("nonexistent", "jdoe", "password", ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceNotFound);
    }

    [Fact]
    public async Task Handle_WhenSourceDisabled_ThrowsDisabled()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateLdapSource("corporate-ldap", isEnabled: false);
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new HandleLdapGrantCommand("corporate-ldap", "jdoe", "password", ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceDisabled);
    }

    [Fact]
    public async Task Handle_WhenSourceTypeNotLdap_ThrowsTypeMismatch()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "oidc-source",
            Code = "oidc-source",
            DisplayName = "OIDC Source",
            Type = IdentitySourceType.Oidc,
            IsEnabled = true,
            OidcConfig = new IdentitySourceOidcConfig
            {
                Authority = "https://idp.example.com",
                ClientId = "client"
            }
        };
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new HandleLdapGrantCommand("oidc-source", "jdoe", "password", ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceTypeMismatch);
    }

    [Fact]
    public async Task Handle_WhenLdapConfigMissing_ThrowsTypeMismatch()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource
        {
            Name = "ldap-no-config",
            Code = "ldap-no-config",
            DisplayName = "LDAP No Config",
            Type = IdentitySourceType.Ldap,
            IsEnabled = true
        };
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext);

        var act = () => handler.Handle(
            new HandleLdapGrantCommand("ldap-no-config", "jdoe", "password", ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceTypeMismatch);
    }

    [Fact]
    public async Task Handle_WhenLinkNotFound_ThrowsLinkNotFound()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateLdapSource("corporate-ldap");
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var ldapAuthenticator = new Mock<ILdapAuthenticator>();
        ldapAuthenticator
            .Setup(x => x.AuthenticateAsync(It.IsAny<IdentitySourceLdapConfig>(), "jdoe", "password", It.IsAny<CancellationToken>()))
            .ReturnsAsync("jdoe");

        var handler = CreateHandler(dbContext, ldapAuthenticator: ldapAuthenticator);

        var act = () => handler.Handle(
            new HandleLdapGrantCommand("corporate-ldap", "jdoe", "password", ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceLinkNotFound);
    }

    [Fact]
    public async Task Handle_WhenUserInactive_ThrowsUserInactive()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateLdapSource("corporate-ldap");
        var user = new User
        {
            Username = "jdoe",
            Email = "jdoe@example.com",
            PasswordHash = "hash",
            IsActive = false
        };
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
        {
            UserId = user.Id,
            IdentitySourceId = source.Id,
            ExternalIdentity = "jdoe"
        });
        await dbContext.SaveChangesAsync();

        var ldapAuthenticator = new Mock<ILdapAuthenticator>();
        ldapAuthenticator
            .Setup(x => x.AuthenticateAsync(It.IsAny<IdentitySourceLdapConfig>(), "jdoe", "password", It.IsAny<CancellationToken>()))
            .ReturnsAsync("jdoe");

        var handler = CreateHandler(dbContext, ldapAuthenticator: ldapAuthenticator);

        var act = () => handler.Handle(
            new HandleLdapGrantCommand("corporate-ldap", "jdoe", "password", ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceUserInactive);
    }

    [Fact]
    public async Task Handle_WhenValid_ReturnsSuccess()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateLdapSource("corporate-ldap");
        var user = new User
        {
            Username = "jdoe",
            Email = "jdoe@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        dbContext.IdentitySources.Add(source);
        dbContext.Users.Add(user);
        dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
        {
            UserId = user.Id,
            IdentitySourceId = source.Id,
            ExternalIdentity = "jdoe"
        });
        await dbContext.SaveChangesAsync();

        var ldapAuthenticator = new Mock<ILdapAuthenticator>();
        ldapAuthenticator
            .Setup(x => x.AuthenticateAsync(It.IsAny<IdentitySourceLdapConfig>(), "jdoe", "password", It.IsAny<CancellationToken>()))
            .ReturnsAsync("jdoe");

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", user.Id.ToString())]));
        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(
                It.Is<BuildPrincipalQuery>(q => q.UserId == user.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(principal);

        var handler = CreateHandler(dbContext, sender: senderMock, ldapAuthenticator: ldapAuthenticator);

        var result = await handler.Handle(
            new HandleLdapGrantCommand("corporate-ldap", "jdoe", "password", ["openid"]),
            CancellationToken.None);

        result.Should().BeOfType<CredentialValidationResult.Success>();
        var success = (CredentialValidationResult.Success)result;
        success.Principal.Should().Be(principal);
    }

    [Fact]
    public async Task Handle_WhenMfaRequired_ReturnsMfaRequired()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateLdapSource("corporate-ldap");
        var user = new User
        {
            Username = "jdoe",
            Email = "jdoe@example.com",
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
            ExternalIdentity = "jdoe"
        });
        await dbContext.SaveChangesAsync();

        var ldapAuthenticator = new Mock<ILdapAuthenticator>();
        ldapAuthenticator
            .Setup(x => x.AuthenticateAsync(It.IsAny<IdentitySourceLdapConfig>(), "jdoe", "password", It.IsAny<CancellationToken>()))
            .ReturnsAsync("jdoe");

        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(
                It.Is<CreateLoginChallengeCommand>(c => c.UserId == user.Id && c.Channel == TwoFactorChannel.Email),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorChallenge.Create(user.Id, TwoFactorChallenge.PurposeLogin, TwoFactorChannel.Email, "hash", "salt", "enc", DateTime.UtcNow.AddMinutes(5), 5));

        var handler = CreateHandler(dbContext, sender: senderMock, ldapAuthenticator: ldapAuthenticator);

        var result = await handler.Handle(
            new HandleLdapGrantCommand("corporate-ldap", "jdoe", "password", ["openid"]),
            CancellationToken.None);

        result.Should().BeOfType<CredentialValidationResult.MfaRequired>();
    }

    [Fact]
    public async Task Handle_WhenLdapAuthFails_ThrowsTokenInvalid()
    {
        await using var dbContext = CreateDbContext();
        var source = CreateLdapSource("corporate-ldap");
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();

        var ldapAuthenticator = new Mock<ILdapAuthenticator>();
        ldapAuthenticator
            .Setup(x => x.AuthenticateAsync(It.IsAny<IdentitySourceLdapConfig>(), "jdoe", "wrong", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthException(AuthErrorCatalog.IdentitySourceTokenInvalid));

        var handler = CreateHandler(dbContext, ldapAuthenticator: ldapAuthenticator);

        var act = () => handler.Handle(
            new HandleLdapGrantCommand("corporate-ldap", "jdoe", "wrong", ["openid"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceTokenInvalid);
    }

    private static IdentitySource CreateLdapSource(string name, bool isEnabled = true) => new()
    {
        Name = name,
        Code = name,
        DisplayName = name,
        Type = IdentitySourceType.Ldap,
        IsEnabled = isEnabled,
        LdapConfig = new IdentitySourceLdapConfig
        {
            Host = "ldap.example.com",
            Port = 389,
            BaseDn = "dc=example,dc=com",
            BindDn = "cn=admin,dc=example,dc=com",
            BindPassword = "admin-password",
            UseSsl = false,
            SearchFilter = "(uid={username})"
        }
    };

    private static HandleLdapGrantCommandHandler CreateHandler(
        AuthDbContext dbContext,
        Mock<ISender>? sender = null,
        Mock<ILdapAuthenticator>? ldapAuthenticator = null)
    {
        sender ??= new Mock<ISender>();
        ldapAuthenticator ??= new Mock<ILdapAuthenticator>();

        return new HandleLdapGrantCommandHandler(
            dbContext,
            sender.Object,
            ldapAuthenticator.Object);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuthDbContext(options);
    }
}
