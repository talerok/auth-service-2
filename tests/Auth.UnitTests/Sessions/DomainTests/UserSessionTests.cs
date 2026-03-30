using Auth.Domain;
using FluentAssertions;

namespace Auth.UnitTests.Sessions.DomainTests;

public sealed class UserSessionTests
{
    [Fact]
    public void Create_WithValidParams_CreatesSession()
    {
        var userId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        var session = UserSession.Create(userId, "127.0.0.1", "Mozilla/5.0", applicationId, "pwd", 7);

        session.UserId.Should().Be(userId);
        session.IpAddress.Should().Be("127.0.0.1");
        session.UserAgent.Should().Be("Mozilla/5.0");
        session.ApplicationId.Should().Be(applicationId);
        session.AuthMethod.Should().Be("pwd");
        session.IsRevoked.Should().BeFalse();
        session.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(2));
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        session.LastActivityAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithLongUserAgent_Truncates()
    {
        var longUa = new string('x', 600);

        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", longUa, null, "pwd", 7);

        session.UserAgent.Should().HaveLength(500);
    }

    [Fact]
    public void Create_WithNullApplicationId_AllowsNull()
    {
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);

        session.ApplicationId.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyIpAddress_Throws(string? ip)
    {
        var act = () => UserSession.Create(Guid.NewGuid(), ip!, "UA", null, "pwd", 7);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyUserAgent_Throws(string? ua)
    {
        var act = () => UserSession.Create(Guid.NewGuid(), "127.0.0.1", ua!, null, "pwd", 7);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyAuthMethod_Throws(string? method)
    {
        var act = () => UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, method!, 7);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsActive_WhenNotRevokedAndNotExpired_ReturnsTrue()
    {
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);

        session.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenRevoked_ReturnsFalse()
    {
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);
        session.Revoke("test");

        session.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithZeroOrNegativeLifetime_Throws(int days)
    {
        var act = () => UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", days);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        var act = () => UserSession.Create(Guid.Empty, "127.0.0.1", "UA", null, "pwd", 7);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TouchActivity_UpdatesLastActivityAt()
    {
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);
        var before = session.LastActivityAt;

        session.TouchActivity();

        session.LastActivityAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Revoke_SetsRevokedFields()
    {
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);

        session.Revoke("admin");

        session.IsRevoked.Should().BeTrue();
        session.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        session.RevokedReason.Should().Be("admin");
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_IsIdempotent()
    {
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);
        session.Revoke("first");
        var firstRevokedAt = session.RevokedAt;

        session.Revoke("second");

        session.RevokedReason.Should().Be("first");
        session.RevokedAt.Should().Be(firstRevokedAt);
    }


    [Fact]
    public void Create_WithIpv6Address_Stores()
    {
        var session = UserSession.Create(Guid.NewGuid(), "::1", "UA", null, "pwd", 7);

        session.IpAddress.Should().Be("::1");
    }

    [Fact]
    public void Create_WithFullIpv6Address_Stores()
    {
        var ipv6 = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";

        var session = UserSession.Create(Guid.NewGuid(), ipv6, "UA", null, "pwd", 7);

        session.IpAddress.Should().Be(ipv6);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Revoke_WithInvalidReason_Throws(string? reason)
    {
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);

        var act = () => session.Revoke(reason!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Revoke_WithLongReason_Truncates()
    {
        var session = UserSession.Create(Guid.NewGuid(), "127.0.0.1", "UA", null, "pwd", 7);
        var longReason = new string('r', 200);

        session.Revoke(longReason);

        session.RevokedReason.Should().HaveLength(100);
    }
}
