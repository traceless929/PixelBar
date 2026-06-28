namespace PixelBar.Sdk.Protocol;

/// <summary>
/// TempoHub official light report (opcode 0x77).
/// Captured from EDIFIER TempoHub HID traffic.
/// </summary>
public static class TempoHubLightReportEncoder
{
    private static ReadOnlySpan<byte> Header => [0x2E, 0xAA, 0xEC, 0x77];
    private static ReadOnlySpan<byte> FixedPrefix => [0x00, 0x09, 0x07, 0xEA, 0x06, 0x1C, 0x10];

    public const byte DefaultBrightness = 0x18;
    public const byte DefaultTail = 0x60;

    /// <summary>
    /// Build the TempoHub 0x77 light packet.
    /// Bytes 11-12 encode color in device color space (approximated from RGB).
    /// </summary>
    public static byte[] Encode(RgbColor color, byte speed = RgbReportEncoder.DefaultSpeed, byte brightness = DefaultBrightness)
    {
        Span<byte> scratch = stackalloc byte[17];
        Header.CopyTo(scratch);
        FixedPrefix.CopyTo(scratch[4..]);
        scratch[11] = (byte)(0x1C + color.R * 0x0B / 255);
        scratch[12] = (byte)(0x2E + color.G * 0x02 / 255);
        scratch[13] = 0x00;
        scratch[14] = 0x00;
        scratch[15] = brightness;
        scratch[16] = (byte)(DefaultTail + (speed - 1) * 2 + color.B / 32);

        return HidReportFormatter.PadToReportLength(scratch);
    }

    /// <summary>
    /// Replay an exact captured TempoHub template (useful for validation).
    /// </summary>
    public static byte[] EncodeCapturedDefault() =>
        HidReportFormatter.PadToReportLength([
            0x2E, 0xAA, 0xEC, 0x77, 0x00, 0x09, 0x07, 0xEA, 0x06, 0x1C, 0x10,
            0x1C, 0x30, 0x00, 0x00, 0x18, 0x60,
        ]);
}
