namespace PixelBar.Sdk.Protocol;

public static class HidReportLayout
{
    public const int Length = 64;
    public const byte ChecksumSeed = 0xD2;
}

public static class HidReportFormatter
{
    public static string ToHex(ReadOnlySpan<byte> report) =>
        Convert.ToHexString(report).ToLowerInvariant();

    public static byte[] PadToReportLength(ReadOnlySpan<byte> payload)
    {
        if (payload.Length > HidReportLayout.Length)
            throw new ArgumentException($"Payload exceeds {HidReportLayout.Length} bytes.", nameof(payload));

        var report = new byte[HidReportLayout.Length];
        payload.CopyTo(report);
        return report;
    }

    public static byte SumChecksum(ReadOnlySpan<byte> data)
    {
        var total = HidReportLayout.ChecksumSeed;
        foreach (var value in data)
            total += value;

        return (byte)(total & 0xFF);
    }
}
