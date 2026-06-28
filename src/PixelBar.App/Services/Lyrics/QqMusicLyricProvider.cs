using System.Collections.Concurrent;

using System.Text;

using System.Text.RegularExpressions;



namespace PixelBar_App.Services.Lyrics;



public sealed partial class QqMusicLyricProvider

{

    private readonly ConcurrentDictionary<string, IndexedLyricEntry?> _cache = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _indexLock = new();

    private DateTime _indexBuiltAt = DateTime.MinValue;

    private IReadOnlyList<IndexedLyricEntry> _index = [];



    public IReadOnlyList<string> GetSearchDirectories(string? customDirectory) =>

        QqMusicCacheLocator.GetLyricDirectories(customDirectory);



    public string DecryptedCacheDirectory => QqMusicLyricCacheStore.GetCacheDirectory();



    public int DecryptedCacheFileCount => QqMusicLyricCacheStore.CountCachedEntries();

    public void InvalidateIndex()
    {
        lock (_indexLock)
        {
            _indexBuiltAt = DateTime.MinValue;
            _index = [];
        }

        _cache.Clear();
    }

    public LrcDocument? TryFindLyrics(string title, string artist, string? customDirectory)

    {

        EnsureIndex(customDirectory);



        LrcDocument? best = null;

        var bestScore = 0;

        foreach (var entry in _index)

        {

            var score = ScoreMatch(entry, title, artist);

            if (score > bestScore)

            {

                bestScore = score;

                best = entry.Document;

            }

        }



        if (bestScore >= 80)

            return best;



        var recent = _index

            .Where(entry => entry.Modified >= DateTime.Now.AddMinutes(-5))

            .OrderByDescending(entry => entry.Modified)

            .Select(entry => entry.Document)

            .FirstOrDefault(document => document is not null);



        if (recent is not null && bestScore < 40)

            return recent;



        return bestScore >= 40 ? best : recent;

    }



    public LrcDocument? TryFindMostRecent(string? customDirectory)

    {

        EnsureIndex(customDirectory);

        return _index

            .OrderByDescending(entry => entry.Modified)

            .Select(entry => entry.Document)

            .FirstOrDefault(document => document is not null);

    }



    public int IndexedFileCount

    {

        get

        {

            lock (_indexLock)

                return _index.Count;

        }

    }



    private void EnsureIndex(string? customDirectory)

    {

        lock (_indexLock)

        {

            if (DateTime.UtcNow - _indexBuiltAt < TimeSpan.FromSeconds(20))

                return;



            var entries = new List<IndexedLyricEntry>();

            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);



            foreach (var dir in GetSearchDirectories(customDirectory))

            {

                try

                {

                    foreach (var path in Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly))

                    {

                        var extension = Path.GetExtension(path);

                        if (!extension.Equals(".lrc", StringComparison.OrdinalIgnoreCase)

                            && !extension.Equals(".qrc", StringComparison.OrdinalIgnoreCase)

                            && !path.EndsWith("_qm.qrc", StringComparison.OrdinalIgnoreCase))

                        {

                            continue;

                        }



                        var entry = LoadEntry(path);

                        if (entry?.Document is not null && seenKeys.Add(entry.SourceKey))

                            entries.Add(entry);

                    }

                }

                catch

                {

                    // ignore unreadable directories

                }

            }



            foreach (var (contentHash, meta) in QqMusicLyricCacheStore.EnumerateEntries())

            {

                if (!seenKeys.Add($"cache:{contentHash}"))

                    continue;



                var entry = LoadCachedEntry(contentHash, meta);

                if (entry?.Document is not null)

                    entries.Add(entry);

            }



            _index = entries;

            _indexBuiltAt = DateTime.UtcNow;

        }

    }



    private IndexedLyricEntry? LoadEntry(string path)

    {

        try

        {

            var modified = File.GetLastWriteTimeUtc(path);

            var memoryKey = $"{path}|{modified.Ticks}";

            if (_cache.TryGetValue(memoryKey, out var cached))

                return cached;



            var (title, artist) = ParseFileNameMetadata(path);

            LrcDocument? document;



            if (path.EndsWith(".lrc", StringComparison.OrdinalIgnoreCase))

            {

                document = LrcParser.Parse(File.ReadAllText(path, Encoding.UTF8));

            }

            else if (IsEncryptedQrcPath(path))

            {

                document = LoadEncryptedQrc(path, modified, title, artist);

            }

            else

            {

                document = QrcLyricParser.TryParseFile(path);

            }



            document = FinalizeDocument(document, title, artist);

            var sourceKey = IsEncryptedQrcPath(path)

                ? $"qrc:{path}|{modified.Ticks}"

                : $"file:{path}|{modified.Ticks}";



            var indexed = document is null

                ? null

                : new IndexedLyricEntry(sourceKey, path, modified, title, artist, document);

            _cache[memoryKey] = indexed;

            return indexed;

        }

        catch

        {

            return null;

        }

    }



    private static LrcDocument? LoadEncryptedQrc(string path, DateTime modifiedUtc, string? title, string? artist)

    {

        var encrypted = File.ReadAllBytes(path);

        var contentHash = QqMusicLyricCacheStore.ComputeContentHash(encrypted);



        var xml = QqMusicLyricCacheStore.TryReadDecryptedXml(contentHash);

        if (xml is null)

        {

            xml = QmQrcDecoder.TryDecrypt(encrypted);

            if (xml is null)

                return QrcLyricParser.TryParseFile(path);



            QqMusicLyricCacheStore.Write(contentHash, xml, new QqMusicLyricCacheMeta

            {

                ContentHash = contentHash,

                SourcePath = path,

                SourceFileName = Path.GetFileName(path),

                SourceLength = encrypted.Length,

                SourceLastWriteUtc = modifiedUtc,

                Title = title,

                Artist = artist,

                DecryptedAtUtc = DateTime.UtcNow,

            });

        }



        return QrcLyricParser.TryParse(xml);

    }



    private static IndexedLyricEntry? LoadCachedEntry(string contentHash, QqMusicLyricCacheMeta meta)

    {

        var xml = QqMusicLyricCacheStore.TryReadDecryptedXml(contentHash);

        if (xml is null)

            return null;



        var document = FinalizeDocument(QrcLyricParser.TryParse(xml), meta.Title, meta.Artist);

        if (document is null)

            return null;



        var modified = meta.DecryptedAtUtc;

        return new IndexedLyricEntry(

            $"cache:{contentHash}",

            meta.SourcePath ?? Path.Combine(QqMusicLyricCacheStore.GetCacheDirectory(), $"{contentHash}.xml"),

            modified,

            meta.Title ?? document.Title,

            meta.Artist ?? document.Artist,

            document);

    }



    private static LrcDocument? FinalizeDocument(LrcDocument? document, string? title, string? artist)

    {

        if (document is null || document.Lines.Count == 0)

            return null;



        return new LrcDocument

        {

            Title = document.Title ?? title,

            Artist = document.Artist ?? artist,

            OffsetMs = document.OffsetMs,

            Lines = document.Lines,

        };

    }



    private static bool IsEncryptedQrcPath(string path) =>

        path.EndsWith("_qm.qrc", StringComparison.OrdinalIgnoreCase)

        || path.EndsWith("_qmts.qrc", StringComparison.OrdinalIgnoreCase)

        || path.EndsWith("_qmRoma.qrc", StringComparison.OrdinalIgnoreCase);



    internal static (string? Title, string? Artist) ParseFileNameMetadata(string path)

    {

        var name = Path.GetFileName(path);

        name = name

            .Replace("_qmts.qrc", string.Empty, StringComparison.OrdinalIgnoreCase)

            .Replace("_qmRoma.qrc", string.Empty, StringComparison.OrdinalIgnoreCase)

            .Replace("_qm.qrc", string.Empty, StringComparison.OrdinalIgnoreCase)

            .Replace(".qrc", string.Empty, StringComparison.OrdinalIgnoreCase)

            .Replace(".lrc", string.Empty, StringComparison.OrdinalIgnoreCase);



        var match = FileNameRegex().Match(name);

        if (!match.Success)

            return (null, null);



        var artist = NormalizeFileToken(match.Groups["artist"].Value);

        var title = NormalizeFileToken(match.Groups["title"].Value);

        return (title, artist);

    }



    private static string NormalizeFileToken(string value) =>

        value.Replace('_', ' ').Trim();



    [GeneratedRegex(@"^(?<artist>.+?) - (?<title>.+?) - \d+ - .+$")]

    private static partial Regex FileNameRegex();



    private static int ScoreMatch(IndexedLyricEntry entry, string title, string artist)

    {

        var score = 0;

        if (!string.IsNullOrWhiteSpace(entry.Title) && TextMatch(entry.Title, title))

            score += 60;

        if (!string.IsNullOrWhiteSpace(entry.Artist) && TextMatch(entry.Artist, artist))

            score += 40;

        if (!string.IsNullOrWhiteSpace(entry.Document.Title) && TextMatch(entry.Document.Title, title))

            score += 40;

        if (!string.IsNullOrWhiteSpace(entry.Document.Artist) && TextMatch(entry.Document.Artist, artist))

            score += 30;

        return score;

    }



    private static bool TextMatch(string left, string right)

    {

        var a = Normalize(left);

        var b = Normalize(right);

        if (a.Length == 0 || b.Length == 0)

            return false;



        return a.Equals(b, StringComparison.Ordinal)

            || a.Contains(b, StringComparison.Ordinal)

            || b.Contains(a, StringComparison.Ordinal);

    }



    private static string Normalize(string value)

    {

        var chars = value.Where(static c => !char.IsWhiteSpace(c) && c is not '-' and not '_' and not '（' and not '）' and not '(' and not ')').ToArray();

        return new string(chars).ToLowerInvariant();

    }



    private sealed record IndexedLyricEntry(

        string SourceKey,

        string Path,

        DateTime Modified,

        string? Title,

        string? Artist,

        LrcDocument Document);

}

