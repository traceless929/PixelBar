using System.Text;
using PixelBar.Sdk;
using PixelBar.Sdk.Protocol;

namespace PixelBar_App.Services.Lyrics;

public static class LyricsDisplayFormatter
{
    public const int ScrollUtf8Threshold = 36;

    public static void Show(
        PixelBarClient client,
        string text,
        bool scrollLongLines,
        TextScrollDirection scrollDirection = TextScrollDirection.LeftToRight,
        LyricAnimation animation = LyricAnimation.None)
    {
        text = text.Trim();
        if (text.Length == 0)
            return;

        if (animation != LyricAnimation.None)
        {
            client.ShowLyric(TruncateToUtf8Bytes(text, LyricReportEncoder.MaxUtf8Bytes), animation);
            return;
        }

        client.DisableLyricAnimation();

        var utf8Length = Encoding.UTF8.GetByteCount(text);
        if (utf8Length > TextReportEncoder.MaxUtf8Bytes)
            text = TruncateToUtf8Bytes(text, TextReportEncoder.MaxUtf8Bytes);

        if (scrollLongLines && Encoding.UTF8.GetByteCount(text) > ScrollUtf8Threshold)
        {
            client.ShowText(text, TextDisplayEffect.Scroll, scrollDirection: scrollDirection);
            return;
        }

        client.ShowText(text, TextDisplayEffect.Static, PixelTextAlignment.Center);
    }

    public static string TruncateToUtf8Bytes(string text, int maxBytes)
    {
        if (Encoding.UTF8.GetByteCount(text) <= maxBytes)
            return text;

        var builder = new StringBuilder(text.Length);
        var used = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var size = rune.Utf8SequenceLength;
            if (used + size > maxBytes)
                break;

            builder.Append(rune.ToString());
            used += size;
        }

        return builder.ToString();
    }
}
