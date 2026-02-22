using Auth.Application;
using FluentAssertions;

namespace Auth.UnitTests;

public sealed class CollectionDiffTests
{
    [Fact]
    public void Calculate_WhenDesiredAndCurrentDiffer_ReturnsExpectedToAddAndToRemove()
    {
        var desired = new[] { 1, 3, 4 };
        var current = new[] { 1, 2, 3 };

        var diff = CollectionDiff.Calculate(desired, current);

        diff.ToAdd.Should().BeEquivalentTo([4]);
        diff.ToRemove.Should().BeEquivalentTo([2]);
    }

    [Fact]
    public void Calculate_WhenDesiredContainsDuplicates_ThrowsAuthException()
    {
        var desired = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.Empty };
        var current = Array.Empty<Guid>();

        var act = () => CollectionDiff.Calculate(desired, current);

        act.Should().Throw<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.DuplicateIdsNotAllowed);
    }
}
