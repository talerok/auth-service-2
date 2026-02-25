using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Auth.Domain;
using Auth.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Auth.UnitTests;

public sealed class JwtTokenFactoryTests
{
    [Fact]
    public void CreateTokens_ValidInput_EmbedsWorkspaceMaskClaim()
    {
        var options = Options.Create(new IntegrationOptions
        {
            Jwt = new JwtOptions
            {
                Secret = "super-secret-key-min-32-characters-long!",
                Issuer = "auth-service",
                Audience = "auth-service-clients",
                AccessTokenExpirationMinutes = 15,
                RefreshTokenExpirationDays = 7
            }
        });
        var factory = new JwtTokenFactory(options);

        const string workspaceCode = "default";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            IsActive = true
        };
        var masks = new Dictionary<string, byte[]>
        {
            [workspaceCode] = [0b_0000_0101]
        };

        var tokens = factory.CreateTokens(user, masks);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        var wsClaim = jwt.Claims.First(c => c.Type == "ws").Value;
        var wsPayload = JsonSerializer.Deserialize<Dictionary<string, string>>(wsClaim);

        wsPayload.Should().NotBeNull();
        wsPayload!.Should().ContainKey(workspaceCode);
        wsPayload[workspaceCode].Should().Be(Convert.ToBase64String([0b_0000_0101]));
        jwt.Issuer.Should().Be("auth-service");
        jwt.Audiences.Should().Contain("auth-service-clients");
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }
}
