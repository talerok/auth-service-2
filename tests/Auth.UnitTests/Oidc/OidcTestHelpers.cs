using Auth.Domain;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Auth.UnitTests.Oidc;

internal static class OidcTestHelpers
{
    public static readonly User TestUser = new()
    {
        Id = Guid.NewGuid(),
        Username = "testuser",
        FullName = "Test User",
        Email = "test@example.com",
        Phone = "+1234567890",
        PasswordHash = "hash",
        IsActive = true,
        IsInternalAuthEnabled = true
    };

    public static Mock<IHttpContextAccessor> CreateHttpContextAccessor()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers["User-Agent"] = "TestAgent/1.0";
        var mock = new Mock<IHttpContextAccessor>();
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock;
    }
}
