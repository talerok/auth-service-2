using Auth.Infrastructure;
using FluentAssertions;

namespace Auth.UnitTests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void HashAndVerify_WhenPasswordMatches_ReturnsTrue()
    {
        var hasher = new PasswordHasher();
        const string password = "super-secret";
        var hash = hasher.Hash(password);

        hasher.Verify(password, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WhenPasswordDoesNotMatch_ReturnsFalse()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("super-secret");

        hasher.Verify("wrong-password", hash).Should().BeFalse();
    }
}
