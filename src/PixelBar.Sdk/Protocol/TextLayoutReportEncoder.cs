namespace PixelBar.Sdk.Protocol;

/// <summary>文字显示效果。</summary>
public enum TextDisplayEffect : byte
{
    Static = 0,
    Scroll = 1,
}

/// <summary>文字对齐（仅静态显示有效）。</summary>
public enum PixelTextAlignment : byte
{
    Left = 0,
    Center = 1,
    Right = 2,
    Justify = 3,
}

/// <summary>滚动方向（动态显示时使用）。</summary>
public enum TextScrollDirection : byte
{
    LeftToRight = 0,
    RightToLeft = 1,
}

/// <summary>文字布局包（opcode 0xEF，须在 0xE8 文字包之前发送）。</summary>
public static class TextLayoutReportEncoder
{
    private static ReadOnlySpan<byte> Header => [0x2E, 0xAA, 0xEC, 0xEF];
    private static ReadOnlySpan<byte> BodyPrefix => [0x00, 0x09, 0x01, 0xF0, 0xB4, 0xC8, 0x00, 0x02];

    public static byte[] EncodeStatic(PixelTextAlignment alignment) =>
        Encode(TextDisplayEffect.Static, (byte)alignment);

    public static byte[] EncodeScroll(TextScrollDirection direction = TextScrollDirection.LeftToRight) =>
        Encode(TextDisplayEffect.Scroll, (byte)direction);

    public static byte[] Encode(TextDisplayEffect effect, byte subIndex)
    {
        if (subIndex > 3)
            throw new ArgumentOutOfRangeException(nameof(subIndex), subIndex, "Sub-index must be between 0 and 3.");

        var checksum = (ushort)(0x00FC + (byte)effect + subIndex);

        Span<byte> scratch = stackalloc byte[17];
        Header.CopyTo(scratch);
        BodyPrefix.CopyTo(scratch[4..]);
        scratch[12] = (byte)effect;
        scratch[13] = subIndex;
        scratch[14] = 0xFF;
        scratch[15] = (byte)(checksum & 0xFF);
        scratch[16] = (byte)((checksum >> 8) & 0xFF);

        return HidReportFormatter.PadToReportLength(scratch);
    }
}
