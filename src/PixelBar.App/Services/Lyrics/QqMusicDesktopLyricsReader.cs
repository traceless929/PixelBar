using System.Runtime.InteropServices;
using System.Text;

namespace PixelBar_App.Services.Lyrics;

/// <summary>读取 QQ 音乐桌面歌词窗口（TXGuiFoundation）标题作为当前歌词行。</summary>
public static class QqMusicDesktopLyricsReader
{
    private const string DesktopLyricClass = "TXGuiFoundation";

    public static DesktopLyricSnapshot? TryRead()
    {
        var processId = FindQqMusicProcessId();
        if (processId == 0)
            return null;

        DesktopLyricSnapshot? best = null;
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            GetWindowThreadProcessId(hWnd, out var windowProcessId);
            if (windowProcessId != processId)
                return true;

            var className = ReadClassName(hWnd);
            if (!className.Equals(DesktopLyricClass, StringComparison.OrdinalIgnoreCase))
                return true;

            var title = ReadWindowText(hWnd);
            if (string.IsNullOrWhiteSpace(title))
                return true;

            best = ParseTitle(title);
            return false;
        }, IntPtr.Zero);

        return best;
    }

    private static uint FindQqMusicProcessId()
    {
        foreach (var process in System.Diagnostics.Process.GetProcessesByName("QQMusic"))
        {
            try
            {
                return (uint)process.Id;
            }
            finally
            {
                process.Dispose();
            }
        }

        return 0;
    }

    internal static DesktopLyricSnapshot ParseTitle(string title)
    {
        title = title.Trim();
        var separator = title.LastIndexOf(" - ", StringComparison.Ordinal);
        if (separator > 0 && separator < title.Length - 3)
        {
            var line = title[..separator].Trim();
            var artist = title[(separator + 3)..].Trim();
            if (line.Length > 0)
                return new DesktopLyricSnapshot(line, artist, title);
        }

        return new DesktopLyricSnapshot(title, null, title);
    }

    private static string ReadWindowText(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        if (length <= 0)
            return string.Empty;

        var builder = new StringBuilder(length + 1);
        _ = GetWindowText(hWnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string ReadClassName(IntPtr hWnd)
    {
        var builder = new StringBuilder(256);
        _ = GetClassName(hWnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}

public readonly record struct DesktopLyricSnapshot(string Line, string? Artist, string RawTitle);
