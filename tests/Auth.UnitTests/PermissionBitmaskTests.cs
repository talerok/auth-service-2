using Auth.Application;
using FluentAssertions;

namespace Auth.UnitTests;

public sealed class PermissionBitmaskTests
{
    [Fact]
    public void BuildMask_MultipleBits_SetsExpectedFlags()
    {
        var mask = PermissionBitmask.BuildMask([0, 2, 15]);

        PermissionBitmask.HasBit(mask, 0).Should().BeTrue();
        PermissionBitmask.HasBit(mask, 1).Should().BeFalse();
        PermissionBitmask.HasBit(mask, 2).Should().BeTrue();
        PermissionBitmask.HasBit(mask, 15).Should().BeTrue();
    }

    [Fact]
    public void BuildMask_EmptyInput_ReturnsSingleZeroByte()
    {
        var mask = PermissionBitmask.BuildMask([]);

        mask.Should().HaveCount(1);
        mask[0].Should().Be(0);
    }
}
