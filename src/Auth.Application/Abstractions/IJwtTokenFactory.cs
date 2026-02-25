using Auth.Domain;

namespace Auth.Application;

public interface IJwtTokenFactory
{
    AuthTokensResponse CreateTokens(User user, Dictionary<string, byte[]> workspaceMasks);
    string CreateRefreshToken();
}
