using System.Text;

namespace PixelBar.Sdk.Protocol;

public static class TextReportEncoder
{
    private static ReadOnlySpan<byte> Header => [0x2E, 0xAA, 0xEC, 0xE8];

    public const int MaxUtf8Bytes = 54;

    public static byte[] Encode(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        var utf8 = Encoding.UTF8.GetBytes(text);
        if (utf8.Length > MaxUtf8Bytes)
            throw new ArgumentException($"Text UTF-8 payload must be <= {MaxUtf8Bytes} bytes.", nameof(text));

        var total = utf8.Length + 2;
        var bodyLength = 8 + utf8.Length;

        Span<byte> scratch = stackalloc byte[bodyLength + 1];
        Header.CopyTo(scratch);
        scratch[4] = (byte)((total >> 8) & 0xFF);
        scratch[5] = (byte)(total & 0xFF);
        scratch[6] = 0x00;
        scratch[7] = (byte)utf8.Length;
        utf8.CopyTo(scratch[8..]);
        scratch[bodyLength] = HidReportFormatter.SumChecksum(scratch[..bodyLength]);

        return HidReportFormatter.PadToReportLength(scratch);
    }
}
