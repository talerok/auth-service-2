using Auth.Domain;

namespace Auth.Application;

public interface IJwtTokenFactory
{
    AuthTokensResponse CreateTokens(User user, Dictionary<Guid, byte[]> workspaceMasks);
    string CreateRefreshToken();
}
