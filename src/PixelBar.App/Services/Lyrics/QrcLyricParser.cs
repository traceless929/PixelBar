using System.Text.RegularExpressions;
using System.Xml;

namespace PixelBar_App.Services.Lyrics;

public static partial class QrcLyricParser
{
    [GeneratedRegex(@"LyricContent\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex LyricContentRegex();

    [GeneratedRegex(@"\[(\d+)\s*,\s*(\d+)\]")]
    private static partial Regex LineTagRegex();

    [GeneratedRegex(@"\[(\w+)\s*:\s*([^\]]*)\]")]
    private static partial Regex MetaTagRegex();

    [GeneratedRegex(@"\(\d+\s*,\s*\d+\)")]
    private static partial Regex WordTimingParenRegex();

    public static LrcDocument? TryParseFile(string path)
    {
        try
        {
            var xml = File.ReadAllText(path);
            return TryParseXml(xml);
        }
        catch
        {
            return null;
        }
    }

    public static LrcDocument? TryParseXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        var content = ExtractLyricContent(xml);
        return content is null ? null : TryParseContent(content);
    }

    public static LrcDocument? TryParse(string xmlOrContent) =>
        TryParseXml(xmlOrContent) ?? TryParseContent(xmlOrContent);

    private static string? ExtractLyricContent(string xml)
    {
        if (xml.Contains("LyricContent", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var nodes = doc.SelectNodes("//*[@LyricContent]");
                if (nodes is not null)
                {
                    foreach (XmlNode node in nodes)
                    {
                        var value = node.Attributes?["LyricContent"]?.Value;
                        if (!string.IsNullOrWhiteSpace(value))
                            return value;
                    }
                }
            }
            catch
            {
                // fall through to regex
            }
        }

        var lyricMatch = LyricContentRegex().Match(xml);
        return lyricMatch.Success ? UnescapeXml(lyricMatch.Groups[1].Value) : null;
    }

    private static LrcDocument? TryParseContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        content = UnescapeXml(content);

        string? title = null;
        string? artist = null;
        var lines = new List<LyricLine>();

        foreach (Match match in MetaTagRegex().Matches(content))
        {
            var key = match.Groups[1].Value.ToLowerInvariant();
            var value = match.Groups[2].Value.Trim();
            switch (key)
            {
                case "ti":
                    title = value;
                    break;
                case "ar":
                    artist = value;
                    break;
            }
        }

        var lineMatches = LineTagRegex().Matches(content);
        for (var i = 0; i < lineMatches.Count; i++)
        {
            var current = lineMatches[i];
            if (!int.TryParse(current.Groups[1].Value, out var startMs))
                continue;

            var bodyStart = current.Index + current.Length;
            var bodyEnd = i + 1 < lineMatches.Count ? lineMatches[i + 1].Index : content.Length;
            if (bodyStart >= bodyEnd)
                continue;

            var body = content[bodyStart..bodyEnd];
            body = WordTimingParenRegex().Replace(body, string.Empty);
            body = body.Replace('-', ' ').Trim('\r', '\n', ' ');
            if (body.Length == 0 || IsCreditLine(body))
                continue;

            lines.Add(new LyricLine(TimeSpan.FromMilliseconds(startMs), body));
        }

        if (lines.Count == 0)
            return null;

        lines.Sort(static (a, b) => a.Time.CompareTo(b.Time));
        return new LrcDocument
        {
            Title = title,
            Artist = artist,
            Lines = lines,
        };
    }

    private static bool IsCreditLine(string body)
    {
        if (body.StartsWith("词：", StringComparison.Ordinal)
            || body.StartsWith("词:", StringComparison.Ordinal)
            || body.StartsWith("曲：", StringComparison.Ordinal)
            || body.StartsWith("曲:", StringComparison.Ordinal)
            || body.StartsWith("编曲", StringComparison.Ordinal)
            || body.StartsWith("制作人", StringComparison.Ordinal))
            return true;

        return body.Contains("Ronghao", StringComparison.OrdinalIgnoreCase)
            && body.Length < 24;
    }

    private static string UnescapeXml(string source) =>
        source
            .Replace("&lt;", "<", StringComparison.Ordinal)
            .Replace("&gt;", ">", StringComparison.Ordinal)
            .Replace("&amp;", "&", StringComparison.Ordinal)
            .Replace("&apos;", "'", StringComparison.Ordinal)
            .Replace("&quot;", "\"", StringComparison.Ordinal)
            .Replace("&#x0A;", "\n", StringComparison.Ordinal)
            .Replace("&#10;", "\n", StringComparison.Ordinal);
}
