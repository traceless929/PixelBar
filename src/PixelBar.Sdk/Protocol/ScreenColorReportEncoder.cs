namespace PixelBar.Sdk.Protocol;

/// <summary>
/// Pixel screen theme color (TempoHub「像素屏颜色设置」).
/// Verified from <c>capture/screen_color_log.txt</c>.
/// </summary>
/// <remarks>
/// Single <c>0xEF</c> packet with raw RGB:
/// <c>[2E AA EC EF] [00 04 03] [R] [G] [B] [cs_lo] [cs_hi]</c>
/// <c>checksum = (0x008B + R + G + B - 255) &amp; 0x01FF</c>
/// </remarks>
public static class ScreenColorReportEncoder
{
    private static ReadOnlySpan<byte> Header => [0x2E, 0xAA, 0xEC, 0xEF, 0x00, 0x04, 0x03];

    private const ushort ChecksumBase = 0x008B;

    /// <summary>Build the verified screen-color <c>0xEF</c> packet.</summary>
    public static byte[] Encode(RgbColor color)
    {
        var checksum = (ushort)((ChecksumBase + color.R + color.G + color.B - 255) & 0x01FF);

        Span<byte> scratch = stackalloc byte[12];
        Header.CopyTo(scratch);
        scratch[7] = color.R;
        scratch[8] = color.G;
        scratch[9] = color.B;
        scratch[10] = (byte)(checksum & 0xFF);
        scratch[11] = (byte)(checksum >> 8);

        return HidReportFormatter.PadToReportLength(scratch);
    }

    /// <summary>Set pixel screen color (single packet).</summary>
    public static IEnumerable<byte[]> EncodeSetColor(RgbColor color)
    {
        yield return Encode(color);
    }

    /// <summary>
    /// Also push color to atmosphere light (<c>0x77</c> + <c>0x6B</c>).
    /// TempoHub「同步氛围灯颜色」exact sequence not captured; this mirrors manual light set.
    /// </summary>
    public static IEnumerable<byte[]> EncodeSetColorWithAtmosphereSync(
        RgbColor color,
        LightMode mode = LightMode.PureStatic,
        byte speed = RgbReportEncoder.DefaultSpeed)
    {
        yield return Encode(color);
        yield return TempoHubLightReportEncoder.Encode(color, speed);
        yield return RgbReportEncoder.Encode(mode, color, speed);
    }
}
