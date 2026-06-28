using Microsoft.Win32;

namespace QrcDecryptTest;

internal static class QqMusicPath
{
    public static string? TryGetLyricDirectory()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Tencent\QQMusic\LogConfig");
            var cachePath = key?.GetValue("CACHEPATH") as string;
            if (string.IsNullOrWhiteSpace(cachePath))
                return null;

            var lyricDir = Path.Combine(cachePath.TrimEnd('\\', '/'), "QQMusicLyricNew");
            return Directory.Exists(lyricDir) ? lyricDir : null;
        }
        catch
        {
            return null;
        }
    }
}
