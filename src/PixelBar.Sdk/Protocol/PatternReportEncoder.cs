namespace PixelBar.Sdk.Protocol;

public static class PatternReportEncoder
{
    private static ReadOnlySpan<byte> Header => [0x2E, 0xAA, 0xEC, 0xEF];
    private static ReadOnlySpan<byte> BodyPrefix => [0x00, 0x09, 0x01, 0xF0, 0xB4, 0xC8, 0x00, 0x01];

    public const int MinPattern = 1;
    public const int MaxPattern = 11;

    public static byte[] Encode(int pattern)
    {
        if (pattern is < MinPattern or > MaxPattern)
            throw new ArgumentOutOfRangeException(nameof(pattern), pattern, $"Pattern must be between {MinPattern} and {MaxPattern}.");

        var index = pattern - 1;
        var checksum = (ushort)((0xFFFB + index) & 0xFFFF);

        Span<byte> scratch = stackalloc byte[17];
        Header.CopyTo(scratch);
        BodyPrefix.CopyTo(scratch[4..]);
        scratch[12] = (byte)((index >> 8) & 0xFF);
        scratch[13] = (byte)(index & 0xFF);
        scratch[14] = 0xFF;
        scratch[15] = (byte)(checksum & 0xFF);
        scratch[16] = (byte)((checksum >> 8) & 0xFF);

        return HidReportFormatter.PadToReportLength(scratch);
    }
}
