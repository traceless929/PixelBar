namespace PixelBar.Sdk.Protocol;

public enum LightMode : byte
{
    AmbientBreathing = 1,
    ColorfulTide = 2,
    PureStatic = 3,
    ColorfulRipple = 4,
    FlowingLight = 5,
    DynamicShadow = 6,
}

public readonly record struct RgbColor(byte R, byte G, byte B)
{
    public static RgbColor FromHex(string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);

        var value = hex.TrimStart('#');
        if (value.Length != 6)
            throw new FormatException("Color must be a 6-digit hex value, e.g. #FF0000.");

        return new RgbColor(
            Convert.ToByte(value[..2], 16),
            Convert.ToByte(value[2..4], 16),
            Convert.ToByte(value[4..6], 16));
    }
}

public static class RgbReportEncoder
{
    private static ReadOnlySpan<byte> Header => [0x2E, 0xAA, 0xEC, 0x6B];
    private static ReadOnlySpan<byte> FixedPrefix => [0x00, 0x07, 0x13];
    public const byte DefaultBrightness = 0x3C;
    public const byte DefaultSpeed = 16;
    public const byte MinSpeed = 1;
    public const byte MaxSpeed = 16;

    /// <summary>仅 mode 1-2 支持速度调节。</summary>
    public static bool SupportsSpeed(LightMode mode) =>
        mode is LightMode.AmbientBreathing or LightMode.ColorfulTide;

    public static byte[] Encode(LightMode mode, RgbColor color, byte speed = DefaultSpeed, byte brightness = DefaultBrightness)
    {
        if (SupportsSpeed(mode))
        {
            if (speed is < MinSpeed or > MaxSpeed)
                throw new ArgumentOutOfRangeException(nameof(speed), speed, $"Speed must be between {MinSpeed} and {MaxSpeed}.");
        }
        else
        {
            speed = DefaultSpeed;
        }

        Span<byte> scratch = stackalloc byte[14];
        Header.CopyTo(scratch);
        FixedPrefix.CopyTo(scratch[4..]);
        scratch[7] = (byte)mode;
        scratch[8] = color.R;
        scratch[9] = color.G;
        scratch[10] = color.B;
        scratch[11] = brightness;
        scratch[12] = speed;
        scratch[13] = HidReportFormatter.SumChecksum(scratch[..13]);

        return HidReportFormatter.PadToReportLength(scratch);
    }
}
