namespace Auth.Application;

public static class PermissionBitmask
{
    public static byte[] BuildMask(IEnumerable<int> bits)
    {
        var values = bits.Distinct().ToArray();
        if (values.Length == 0)
        {
            return [0];
        }

        var max = values.Max();
        var bytes = new byte[(max / 8) + 1];
        foreach (var bit in values)
        {
            bytes[bit / 8] |= (byte)(1 << (bit % 8));
        }

        return bytes;
    }

    public static bool HasBit(byte[] bytes, int bit)
    {
        var index = bit / 8;
        if (index >= bytes.Length)
        {
            return false;
        }

        return (bytes[index] & (1 << (bit % 8))) != 0;
    }
}
