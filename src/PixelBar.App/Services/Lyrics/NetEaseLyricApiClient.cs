using System.Net.Http;
using System.Text.Json;

namespace PixelBar_App.Services.Lyrics;

/// <summary>
/// 通过网易云公开 lyric API 按数字歌曲 ID 拉取歌词（Lyrix / Lyricify 同类思路）。
/// </summary>
public static class NetEaseLyricApiClient
{
    private static readonly HttpClient HttpClient = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36" },
            { "Referer", "https://music.163.com/" },
        },
    };

    public static string GetApiCacheDirectory()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PixelBar",
            "NetEaseLyricCache");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static async Task<LrcDocument?> TryFetchLyricsAsync(
        string numericSongId,
        string? title,
        string? artist,
        CancellationToken cancellationToken = default)
    {
        if (!InfLinkSmtcParser.IsNumericSongId(numericSongId))
            return null;

        var cachePath = Path.Combine(GetApiCacheDirectory(), $"{numericSongId}.json");
        if (TryLoadCachedDocument(cachePath, title, artist) is { } cached)
            return cached;

        try
        {
            // 这些参数表示本地已有版本；1 会让版本恰为 1 的歌词返回空内容。
            var url = $"https://music.163.com/api/song/lyric?id={numericSongId}&lv=-1&kv=-1&tv=-1";
            var json = await HttpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json) || !json.Contains("\"code\"", StringComparison.Ordinal))
                return null;

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("code", out var codeElement) && codeElement.GetInt32() != 200)
                return null;

            var document = TryParseResponse(json, title, artist);
            if (document is null)
                return null;

            await File.WriteAllTextAsync(cachePath, json, cancellationToken).ConfigureAwait(false);
            NetEaseLyricMetaStore.Write(numericSongId, title, artist, cachePath);

            return document;
        }
        catch
        {
            return null;
        }
    }

    private static LrcDocument? TryLoadCachedDocument(string cachePath, string? title, string? artist)
    {
        if (!File.Exists(cachePath))
            return null;

        return PrepareDocument(NetEaseLyricCacheParser.TryParseFile(cachePath), title, artist);
    }

    internal static LrcDocument? TryParseResponse(string json, string? title, string? artist) =>
        PrepareDocument(NetEaseLyricCacheParser.TryParseContent(json), title, artist);

    private static LrcDocument? PrepareDocument(LrcDocument? document, string? title, string? artist)
    {
        if (document is null || document.Lines.Count == 0)
            return null;

        var singable = LyricMetadataFilter.FilterSingableLines(document.Lines);
        if (singable.Count == 0)
            return null;

        return new LrcDocument
        {
            Title = document.Title ?? title,
            Artist = document.Artist ?? artist,
            OffsetMs = document.OffsetMs,
            Lines = singable,
        };
    }
}
