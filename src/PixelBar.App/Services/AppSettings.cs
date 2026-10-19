namespace PixelBar_App.Services;

using PixelBar_App.Services.Lyrics;
using PixelBar.Sdk.Protocol;

public sealed class AppSettings
{
    public bool RunAtStartup { get; set; }

    public bool MinimizeToTrayOnClose { get; set; }

    public bool HasCompletedWelcome { get; set; }

    public bool AutoSyncClockOnStartup { get; set; } = true;

    public bool LyricsEnabled { get; set; }

    public bool LyricsScrollLongLines { get; set; } = true;

    public LyricAnimation LyricsAnimation { get; set; } = LyricAnimation.BottomToTop;

    /// <summary>相对歌词文件时间轴的偏移（毫秒）：&gt;0 早一些，&lt;0 晚一些。</summary>
    public int LyricsTimingOffsetMs { get; set; }

    /// <summary>长句滚动时是否从右向左（否则为左向右）。</summary>
    public bool LyricsScrollRightToLeft { get; set; }

    public LyricsMusicProvider LyricsProvider { get; set; } = LyricsMusicProvider.QqMusic;

    public string? QqMusicLyricDirectory { get; set; }

    public string? NetEaseLyricDirectory { get; set; }
}
