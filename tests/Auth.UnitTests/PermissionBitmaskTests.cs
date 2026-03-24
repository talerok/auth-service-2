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

    [Fact]
    public void BuildMask_SingleBit0_SetsBit0Only()
    {
        var mask = PermissionBitmask.BuildMask([0]);

        mask.Should().HaveCount(1);
        mask[0].Should().Be(0b0000_0001);
    }

    [Fact]
    public void BuildMask_SingleBit7_SetsBit7Only()
    {
        var mask = PermissionBitmask.BuildMask([7]);

        mask.Should().HaveCount(1);
        mask[0].Should().Be(0b1000_0000);
    }

    [Fact]
    public void BuildMask_Bit8_AllocatesSecondByte()
    {
        var mask = PermissionBitmask.BuildMask([8]);

        mask.Should().HaveCount(2);
        mask[0].Should().Be(0);
        mask[1].Should().Be(0b0000_0001);
    }

    [Fact]
    public void BuildMask_DuplicateBits_DeduplicatesWithoutError()
    {
        var mask = PermissionBitmask.BuildMask([3, 3, 3]);

        mask.Should().HaveCount(1);
        PermissionBitmask.HasBit(mask, 3).Should().BeTrue();
    }

    [Fact]
    public void BuildMask_HighBit_AllocatesCorrectNumberOfBytes()
    {
        var mask = PermissionBitmask.BuildMask([100]);

        mask.Should().HaveCount(13); // (100 / 8) + 1
        PermissionBitmask.HasBit(mask, 100).Should().BeTrue();
        PermissionBitmask.HasBit(mask, 99).Should().BeFalse();
    }

    [Fact]
    public void BuildMask_AllBitsInOneByte_ReturnsFullByte()
    {
        var mask = PermissionBitmask.BuildMask([0, 1, 2, 3, 4, 5, 6, 7]);

        mask.Should().HaveCount(1);
        mask[0].Should().Be(0xFF);
    }

    [Fact]
    public void HasBit_BitBeyondMaskLength_ReturnsFalse()
    {
        var mask = PermissionBitmask.BuildMask([0]);

        PermissionBitmask.HasBit(mask, 100).Should().BeFalse();
    }

    [Fact]
    public void HasBit_EmptyMask_ReturnsFalse()
    {
        var mask = PermissionBitmask.BuildMask([]);

        PermissionBitmask.HasBit(mask, 0).Should().BeFalse();
    }

    [Fact]
    public void BuildMask_BitsAcrossMultipleBytes_SetsCorrectBits()
    {
        var mask = PermissionBitmask.BuildMask([0, 8, 16]);

        mask.Should().HaveCount(3);
        PermissionBitmask.HasBit(mask, 0).Should().BeTrue();
        PermissionBitmask.HasBit(mask, 8).Should().BeTrue();
        PermissionBitmask.HasBit(mask, 16).Should().BeTrue();
        PermissionBitmask.HasBit(mask, 1).Should().BeFalse();
        PermissionBitmask.HasBit(mask, 9).Should().BeFalse();
    }

    [Fact]
    public void BuildMask_UnsortedBits_ProducesSameResultAsSorted()
    {
        var sorted = PermissionBitmask.BuildMask([1, 5, 10]);
        var unsorted = PermissionBitmask.BuildMask([10, 1, 5]);

        unsorted.Should().BeEquivalentTo(sorted);
    }
}
