using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Auth.Application;
using Auth.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure;

public sealed class JwtTokenFactory(IOptions<IntegrationOptions> options) : IJwtTokenFactory
{
    private readonly JwtOptions _jwt = options.Value.Jwt;

    public AuthTokensResponse CreateTokens(User user, Dictionary<string, byte[]> workspaceMasks)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes);
        var wsPayload = workspaceMasks.ToDictionary(x => x.Key, x => Convert.ToBase64String(x.Value));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new("ws", JsonSerializer.Serialize(wsPayload))
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthTokensResponse(accessToken, CreateRefreshToken(), expiresAt);
    }

    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
