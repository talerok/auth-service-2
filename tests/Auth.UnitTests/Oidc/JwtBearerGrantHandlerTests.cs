using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Auth.Application;
using Auth.Application.Auth.Commands.CreateLoginChallenge;
using Auth.Application.Oidc.Commands.HandleJwtBearerGrant;
using Auth.Application.Oidc.Queries.BuildPrincipal;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Oidc.Commands.HandleJwtBearerGrant;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.Oidc;

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
                It.Is<BuildPrincipalQuery>(q => q.UserId == user.Id
                    && q.AuthMethods != null && q.AuthMethods.SequenceEqual(new[] { "fed" })),
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
                It.Is<BuildPrincipalQuery>(q => q.UserId == user.Id
                    && q.AuthMethods != null && q.AuthMethods.SequenceEqual(new[] { "fed" })),
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
            tokenValidator.Object,
            NullLogger<HandleJwtBearerGrantCommandHandler>.Instance);
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

}
