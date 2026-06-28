using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PixelBar_App.Services.Lyrics;

/// <summary>
/// 解密后的 QRC XML 落盘缓存。键为加密源文件内容的 SHA256，便于二次利用与跳过重复解密。
/// </summary>
public static class QqMusicLyricCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string GetCacheDirectory()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PixelBar",
            "LyricCache");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string ComputeContentHash(ReadOnlySpan<byte> encryptedContent) =>
        Convert.ToHexString(SHA256.HashData(encryptedContent));

    public static string? TryReadDecryptedXml(string contentHash)
    {
        var path = GetXmlPath(contentHash);
        if (!File.Exists(path))
            return null;

        try
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }
        catch
        {
            return null;
        }
    }

    public static QqMusicLyricCacheMeta? TryReadMeta(string contentHash)
    {
        var path = GetMetaPath(contentHash);
        if (!File.Exists(path))
            return null;

        try
        {
            return JsonSerializer.Deserialize<QqMusicLyricCacheMeta>(File.ReadAllText(path), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static void Write(string contentHash, string decryptedXml, QqMusicLyricCacheMeta meta)
    {
        var xmlPath = GetXmlPath(contentHash);
        var metaPath = GetMetaPath(contentHash);
        var tempXml = xmlPath + ".tmp";
        var tempMeta = metaPath + ".tmp";

        try
        {
            File.WriteAllText(tempXml, decryptedXml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.WriteAllText(tempMeta, JsonSerializer.Serialize(meta, JsonOptions), new UTF8Encoding(false));
            File.Move(tempXml, xmlPath, overwrite: true);
            File.Move(tempMeta, metaPath, overwrite: true);
        }
        catch
        {
            TryDelete(tempXml);
            TryDelete(tempMeta);
            throw;
        }
    }

    public static IEnumerable<(string ContentHash, QqMusicLyricCacheMeta Meta)> EnumerateEntries()
    {
        var dir = GetCacheDirectory();
        if (!Directory.Exists(dir))
            yield break;

        foreach (var metaPath in Directory.EnumerateFiles(dir, "*.meta.json", SearchOption.TopDirectoryOnly))
        {
            QqMusicLyricCacheMeta? meta;
            try
            {
                meta = JsonSerializer.Deserialize<QqMusicLyricCacheMeta>(File.ReadAllText(metaPath), JsonOptions);
            }
            catch
            {
                continue;
            }

            if (meta is null || string.IsNullOrWhiteSpace(meta.ContentHash))
                continue;

            var xmlPath = GetXmlPath(meta.ContentHash);
            if (!File.Exists(xmlPath))
                continue;

            yield return (meta.ContentHash, meta);
        }
    }

    public static int CountCachedEntries()
    {
        var dir = GetCacheDirectory();
        return Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir, "*.xml", SearchOption.TopDirectoryOnly).Count()
            : 0;
    }

    private static string GetXmlPath(string contentHash) =>
        Path.Combine(GetCacheDirectory(), $"{contentHash}.xml");

    private static string GetMetaPath(string contentHash) =>
        Path.Combine(GetCacheDirectory(), $"{contentHash}.meta.json");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignore cleanup failures
        }
    }
}

public sealed class QqMusicLyricCacheMeta
{
    public required string ContentHash { get; init; }

    public string? SourcePath { get; init; }

    public string? SourceFileName { get; init; }

    public long SourceLength { get; init; }

    public DateTime SourceLastWriteUtc { get; init; }

    public string? Title { get; init; }

    public string? Artist { get; init; }

    public DateTime DecryptedAtUtc { get; init; } = DateTime.UtcNow;
}
