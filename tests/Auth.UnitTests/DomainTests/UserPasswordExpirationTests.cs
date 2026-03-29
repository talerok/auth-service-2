using Auth.Domain;
using FluentAssertions;

namespace Auth.UnitTests.DomainTests;

public sealed class UserPasswordExpirationTests
{
    [Fact]
    public void SetPassword_SetsPasswordChangedAt()
    {
        var user = new User { Username = "alice", Email = "a@b.com", IsActive = true };

        user.SetPassword("hash");

        user.PasswordChangedAt.Should().NotBeNull();
        user.PasswordChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void IsPasswordExpired_WhenMaxAgeDaysZero_ReturnsFalse()
    {
        var user = new User { Username = "alice", Email = "a@b.com", IsActive = true };
        user.SetPassword("hash");

        user.IsPasswordExpired(0).Should().BeFalse();
    }

    [Fact]
    public void IsPasswordExpired_WhenMaxAgeDaysNegative_ReturnsFalse()
    {
        var user = new User { Username = "alice", Email = "a@b.com", IsActive = true };
        user.SetPassword("hash");

        user.IsPasswordExpired(-1).Should().BeFalse();
    }

    [Fact]
    public void IsPasswordExpired_WhenPasswordChangedAtNull_ReturnsFalse()
    {
        var user = new User { Username = "alice", Email = "a@b.com", PasswordHash = "hash", IsActive = true };

        user.IsPasswordExpired(90).Should().BeFalse();
    }

    [Fact]
    public void IsPasswordExpired_WhenNotExpired_ReturnsFalse()
    {
        var user = new User { Username = "alice", Email = "a@b.com", IsActive = true };
        user.SetPassword("hash");

        user.IsPasswordExpired(90).Should().BeFalse();
    }

    [Fact]
    public void IsPasswordExpired_UsesPerUserOverrideOverDefault()
    {
        var user = new User { Username = "alice", Email = "a@b.com", IsActive = true, PasswordMaxAgeDays = 365 };
        user.SetPassword("hash");

        // global default is 1 day which would make it expired soon, but per-user is 365
        user.IsPasswordExpired(1).Should().BeFalse();
    }

    [Fact]
    public void IsPasswordExpired_PerUserZero_DisablesExpiration()
    {
        var user = new User { Username = "alice", Email = "a@b.com", IsActive = true, PasswordMaxAgeDays = 0 };
        user.SetPassword("hash");

        // Even with global default of 1, user override of 0 disables
        user.IsPasswordExpired(1).Should().BeFalse();
    }

    [Fact]
    public void GetPasswordExpirationUnixTimestamp_WhenDisabled_ReturnsNull()
    {
        var user = new User { Username = "alice", Email = "a@b.com", IsActive = true };
        user.SetPassword("hash");

        user.GetPasswordExpirationUnixTimestamp(0).Should().BeNull();
    }

    [Fact]
    public void GetPasswordExpirationUnixTimestamp_WhenPasswordChangedAtNull_ReturnsNull()
    {
        var user = new User { Username = "alice", Email = "a@b.com", PasswordHash = "hash", IsActive = true };

        user.GetPasswordExpirationUnixTimestamp(90).Should().BeNull();
    }

    [Fact]
    public void GetPasswordExpirationUnixTimestamp_WhenActive_ReturnsCorrectTimestamp()
    {
        var user = new User { Username = "alice", Email = "a@b.com", IsActive = true };
        user.SetPassword("hash");

        var result = user.GetPasswordExpirationUnixTimestamp(90);

        result.Should().NotBeNull();
        var expected = new DateTimeOffset(user.PasswordChangedAt!.Value.AddDays(90), TimeSpan.Zero).ToUnixTimeSeconds();
        result.Should().Be(expected);
    }
}
