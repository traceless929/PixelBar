using Microsoft.Win32;

namespace PixelBar_App.Services.Lyrics;

public static class QqMusicCacheLocator
{
    private const string RegistryPath = @"Software\Tencent\QQMusic\LogConfig";

    public static IReadOnlyList<string> GetLyricDirectories(string? customDirectory)
    {
        var directories = new List<string>();
        if (!string.IsNullOrWhiteSpace(customDirectory))
        {
            var trimmed = customDirectory.Trim();
            if (Directory.Exists(trimmed))
                directories.Add(trimmed);
        }

        foreach (var lyricDir in EnumerateDefaultLyricDirectories())
        {
            if (Directory.Exists(lyricDir) && !directories.Contains(lyricDir, StringComparer.OrdinalIgnoreCase))
                directories.Add(lyricDir);
        }

        return directories;
    }

    public static string? GetPrimaryCachePath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
            var cachePath = key?.GetValue("CACHEPATH") as string;
            if (!string.IsNullOrWhiteSpace(cachePath))
                return cachePath.TrimEnd('\\', '/');
        }
        catch
        {
            // ignore registry read failures
        }

        return null;
    }

    private static IEnumerable<string> EnumerateDefaultLyricDirectories()
    {
        var cachePath = GetPrimaryCachePath();
        if (!string.IsNullOrWhiteSpace(cachePath))
        {
            yield return Path.Combine(cachePath, "QQMusicLyricNew");
            yield return Path.Combine(cachePath, "QQMusicLyric");
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        yield return Path.Combine(appData, "Tencent", "QQMusic", "QQMusicLyricNew");
        yield return Path.Combine(appData, "Tencent", "QQMusic", "QQMusicLyric");

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        yield return Path.Combine(documents, "Tencent", "QQMusic", "QQMusicLyricNew");
        yield return Path.Combine(documents, "Tencent", "QQMusic", "QQMusicLyric");
    }
}
