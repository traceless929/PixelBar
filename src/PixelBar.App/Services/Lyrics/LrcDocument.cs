namespace PixelBar_App.Services.Lyrics;

public sealed class LrcDocument
{
    public string? Title { get; init; }

    public string? Artist { get; init; }

    public int OffsetMs { get; init; }

    public required IReadOnlyList<LyricLine> Lines { get; init; }

    public string? GetLineAt(TimeSpan position, int userTimingOffsetMs = 0)
    {
        if (Lines.Count == 0)
            return null;

        var targetMs = position.TotalMilliseconds - userTimingOffsetMs + OffsetMs;
        var index = -1;
        for (var i = 0; i < Lines.Count; i++)
        {
            if (Lines[i].Time.TotalMilliseconds <= targetMs)
                index = i;
            else
                break;
        }

        return index >= 0 ? Lines[index].Text : null;
    }
}
