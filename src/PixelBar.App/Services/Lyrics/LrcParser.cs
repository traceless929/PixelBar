using System.Globalization;
using System.Text.RegularExpressions;

namespace PixelBar_App.Services.Lyrics;

public static partial class LrcParser
{
    [GeneratedRegex(@"^\[(?<min>\d+):(?<sec>\d+(?:[\.:]\d+)?)\](?<text>.*)$")]
    private static partial Regex TimeLineRegex();

    [GeneratedRegex(@"^\[(?<key>ti|ar|al|offset)\:(?<value>.*)\]$", RegexOptions.IgnoreCase)]
    private static partial Regex MetaLineRegex();

    public static LrcDocument Parse(string content)
    {
        string? title = null;
        string? artist = null;
        var offsetMs = 0;
        var lines = new List<LyricLine>();

        foreach (var rawLine in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            var meta = MetaLineRegex().Match(line);
            if (meta.Success)
            {
                var key = meta.Groups["key"].Value.ToLowerInvariant();
                var value = meta.Groups["value"].Value.Trim();
                switch (key)
                {
                    case "ti":
                        title = value;
                        break;
                    case "ar":
                        artist = value;
                        break;
                    case "offset":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var offset))
                            offsetMs = offset;
                        break;
                }

                continue;
            }

            var match = TimeLineRegex().Match(line);
            if (!match.Success)
                continue;

            var text = match.Groups["text"].Value.Trim();
            if (text.Length == 0)
                continue;

            if (!TryParseTimestamp(match.Groups["min"].Value, match.Groups["sec"].Value, out var time))
                continue;

            lines.Add(new LyricLine(time, text));
        }

        lines.Sort(static (a, b) => a.Time.CompareTo(b.Time));
        return new LrcDocument
        {
            Title = title,
            Artist = artist,
            OffsetMs = offsetMs,
            Lines = lines,
        };
    }

    private static bool TryParseTimestamp(string minutesText, string secondsText, out TimeSpan time)
    {
        time = default;
        if (!int.TryParse(minutesText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
            return false;

        secondsText = secondsText.Replace(':', '.');
        if (!double.TryParse(secondsText, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            return false;

        time = TimeSpan.FromMilliseconds(minutes * 60_000 + seconds * 1000);
        return true;
    }
}
