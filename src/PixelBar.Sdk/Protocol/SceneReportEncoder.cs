namespace PixelBar.Sdk.Protocol;

/// <summary>频谱 Tab 的 0xEF 包（magic C0 FF F2）。</summary>
public static class SceneReportEncoder
{
    private static ReadOnlySpan<byte> Header => [0x2E, 0xAA, 0xEC, 0xEF];
    private static ReadOnlySpan<byte> StylePrefix => [0x00, 0x09, 0x01];
    private static ReadOnlySpan<byte> Magic => [0xC0, 0xFF, 0xF2];
    private static ReadOnlySpan<byte> FixedMid => [0x00, 0x01];

    public const int MinSpectrumStyle = 1;
    public const int MaxSpectrumStyle = 4;

    private const byte SpectrumCategoryTab = 8;

    /// <summary>频谱类样式 1–4（0x0800~0x0803，单包即可切换）。</summary>
    public static byte[] EncodeSpectrum(int style)
    {
        if (style is < MinSpectrumStyle or > MaxSpectrumStyle)
            throw new ArgumentOutOfRangeException(nameof(style), style, $"Spectrum style must be between {MinSpectrumStyle} and {MaxSpectrumStyle}.");

        var styleIndex = (byte)(style - 1);
        var checksum = (ushort)(0x0040 + SpectrumCategoryTab + styleIndex);

        Span<byte> scratch = stackalloc byte[17];
        Header.CopyTo(scratch);
        StylePrefix.CopyTo(scratch[4..]);
        Magic.CopyTo(scratch[7..]);
        FixedMid.CopyTo(scratch[10..]);
        scratch[12] = SpectrumCategoryTab;
        scratch[13] = styleIndex;
        scratch[14] = 0xFF;
        scratch[15] = (byte)(checksum & 0xFF);
        scratch[16] = (byte)((checksum >> 8) & 0xFF);

        return HidReportFormatter.PadToReportLength(scratch);
    }
}
