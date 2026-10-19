using System.Text;

namespace PixelBar.Sdk.Protocol;

/// <summary>歌词进入/退出动画；数值为跨型号的逻辑编号。</summary>
public enum LyricAnimation : byte
{
    None = 0,
    BottomToTop = 1,
    ExpandAndShrink = 2,
    LeftToRightThenRightToLeft = 3,
    BottomToTopThenTopToBottom = 4,
    TopToBottomThenRightToLeft = 5,
}

/// <summary>固件歌词动画配置及来源为 0x0E 的歌词文字包。</summary>
public static class LyricReportEncoder
{
    public const int MaxUtf8Bytes = byte.MaxValue;
    public const int FragmentUtf8Bytes = 55;

    /// <summary>启用情绪歌词叠层（action 2）；仅选择动画不能启用歌词文字通道。</summary>
    public static byte[] EncodeEnable(bool enabled = true) =>
        EncodeConfiguration([0x02, 0xF0, 0xB4, 0xC8, 0x00, 0x00, enabled ? (byte)0x01 : (byte)0x00, 0xFF, 0xFF]);

    /// <summary>A106 与本次实测 A160 均使用 EC/EF，固件动画编号为 0–4。</summary>
    public static byte[] EncodeAnimation(LyricAnimation animation)
    {
        if (animation is < LyricAnimation.BottomToTop or > LyricAnimation.TopToBottomThenRightToLeft)
            throw new ArgumentOutOfRangeException(nameof(animation), animation, "Animation must be between 1 and 5.");

        return EncodeConfiguration([0x01, 0xF0, 0xB4, 0xC8, 0x00, 0x00, 0x01, 0x00, (byte)((byte)animation - 1)]);
    }

    private static byte[] EncodeConfiguration(byte[] payload)
    {
        var report = new byte[HidReportLayout.Length];
        report[0] = 0x2E;
        report[1] = 0xAA;
        report[2] = 0xEC;
        report[3] = 0xEF;
        report[5] = (byte)payload.Length;
        payload.CopyTo(report, 6);
        report[6 + payload.Length] = HidReportFormatter.SumChecksum(report.AsSpan(0, 6 + payload.Length));
        return report;
    }

    /// <summary>先启用歌词，再设置动画并发送文字；长句按固件协议分包，总 UTF-8 长度不超过 255 字节。</summary>
    public static IReadOnlyList<byte[]> Encode(string text, LyricAnimation animation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var utf8 = Encoding.UTF8.GetBytes(text);
        if (utf8.Length > MaxUtf8Bytes)
            throw new ArgumentException($"Lyric UTF-8 payload must be <= {MaxUtf8Bytes} bytes.", nameof(text));

        List<byte[]> reports = [EncodeEnable(), EncodeAnimation(animation)];
        for (var offset = 0; offset < utf8.Length; offset += FragmentUtf8Bytes)
        {
            var count = Math.Min(FragmentUtf8Bytes, utf8.Length - offset);
            var report = new byte[HidReportLayout.Length];
            report[0] = 0x2E;
            report[1] = 0xAA;
            report[2] = 0xEC;
            report[3] = 0xE8;
            report[5] = (byte)(count + 2);
            report[6] = 0x0E;
            report[7] = (byte)utf8.Length;
            utf8.AsSpan(offset, count).CopyTo(report.AsSpan(8));
            report[8 + count] = HidReportFormatter.SumChecksum(report.AsSpan(0, 8 + count));
            reports.Add(report);
        }

        return reports;
    }
}
