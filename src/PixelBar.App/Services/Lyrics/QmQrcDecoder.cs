using System.IO.Compression;
using System.Text;

// Algorithm ported from https://github.com/jixunmoe-go/qrc (MIT).
// See THIRD_PARTY_NOTICES.md in the repository root.

namespace PixelBar_App.Services.Lyrics;

public static class QmQrcDecoder
{
    private static readonly byte[] QrcQmcMagic =
    [
        0x98, 0x25, 0xB0, 0xAC, 0xE3, 0x02, 0x83, 0x68, 0xE8, 0xFC, 0x6C,
    ];

    private static readonly byte[] DesKey1 = "!@#)(NHL"u8.ToArray();
    private static readonly byte[] DesKey2 = "123ZXC!@"u8.ToArray();
    private static readonly byte[] DesKey3 = "!@#)(*$%"u8.ToArray();

    private static readonly byte[] Qmc1Key =
    [
        0xc3, 0x4a, 0xd6, 0xca, 0x90, 0x67, 0xf7, 0x52, 0xd8, 0xa1, 0x66, 0x62, 0x9f, 0x5b, 0x09, 0x00,
        0xc3, 0x5e, 0x95, 0x23, 0x9f, 0x13, 0x11, 0x7e, 0xd8, 0x92, 0x3f, 0xbc, 0x90, 0xbb, 0x74, 0x0e,
        0xc3, 0x47, 0x74, 0x3d, 0x90, 0xaa, 0x3f, 0x51, 0xd8, 0xf4, 0x11, 0x84, 0x9f, 0xde, 0x95, 0x1d,
        0xc3, 0xc6, 0x09, 0xd5, 0x9f, 0xfa, 0x66, 0xf9, 0xd8, 0xf0, 0xf7, 0xa0, 0x90, 0xa1, 0xd6, 0xf3,
        0xc3, 0xf3, 0xd6, 0xa1, 0x90, 0xa0, 0xf7, 0xf0, 0xd8, 0xf9, 0x66, 0xfa, 0x9f, 0xd5, 0x09, 0xc6,
        0xc3, 0x1d, 0x95, 0xde, 0x9f, 0x84, 0x11, 0xf4, 0xd8, 0x51, 0x3f, 0xaa, 0x90, 0x3d, 0x74, 0x47,
        0xc3, 0x0e, 0x74, 0xbb, 0x90, 0xbc, 0x3f, 0x92, 0xd8, 0x7e, 0x11, 0x13, 0x9f, 0x23, 0x95, 0x5e,
        0xc3, 0x00, 0x09, 0x5b, 0x9f, 0x62, 0x66, 0xa1, 0xd8, 0x52, 0xf7, 0x67, 0x90, 0xca, 0xd6, 0x4a,
    ];

    public static string? TryDecryptFile(string path)
    {
        try
        {
            return TryDecrypt(File.ReadAllBytes(path));
        }
        catch
        {
            return null;
        }
    }

    public static string? TryDecrypt(ReadOnlySpan<byte> data)
    {
        try
        {
            var decoded = DecodeToBytes(data);
            if (decoded is null || decoded.Length == 0)
                return null;

            var text = Encoding.UTF8.GetString(decoded);
            return IsLikelyLyricPayload(text) ? text : null;
        }
        catch
        {
            return null;
        }
    }

    internal static byte[]? DecodeToBytes(ReadOnlySpan<byte> data)
    {
        byte[] buffer;
        if (HasMagic(data))
        {
            buffer = data.ToArray();
            QmcDecode(buffer);
            buffer = buffer[QrcQmcMagic.Length..];
        }
        else
        {
            buffer = data.ToArray();
        }

        if (buffer.Length == 0 || buffer.Length % 8 != 0)
            return null;

        QmQrcDes.TransformBytes(buffer, DesKey1, encrypt: false);
        QmQrcDes.TransformBytes(buffer, DesKey2, encrypt: true);
        QmQrcDes.TransformBytes(buffer, DesKey3, encrypt: false);

        using var input = new MemoryStream(buffer);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    public static bool HasMagic(ReadOnlySpan<byte> data)
    {
        if (data.Length < QrcQmcMagic.Length)
            return false;

        for (var i = 0; i < QrcQmcMagic.Length; i++)
        {
            if (data[i] != QrcQmcMagic[i])
                return false;
        }

        return true;
    }

    private static void QmcDecode(byte[] data)
    {
        for (var i = 0; i < data.Length; i++)
            data[i] ^= QmcCryptoEncode(i);
    }

    private static byte QmcCryptoEncode(int offset) =>
        offset > 0x7FFF
            ? Qmc1Key[(offset % 0x7FFF) & 0x7F]
            : Qmc1Key[offset & 0x7F];

    private static bool IsLikelyLyricPayload(string text) =>
        !string.IsNullOrWhiteSpace(text)
        && (text.Contains("LyricContent", StringComparison.OrdinalIgnoreCase)
            || text.Contains("[ti:", StringComparison.OrdinalIgnoreCase)
            || text.Contains("<QrcInfos", StringComparison.OrdinalIgnoreCase));
}
