using Windows.Media.Control;

namespace PixelBar_App.Services.Lyrics;

public sealed class MediaSessionMonitor
{
    public async Task<MediaPlaybackInfo?> TryGetQqMusicPlaybackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        foreach (var session in manager.GetSessions())
        {
            var appId = session.SourceAppUserModelId ?? string.Empty;
            if (!IsQqMusic(appId))
                continue;

            return await ReadSessionAsync(session, appId).ConfigureAwait(false);
        }

        return null;
    }

    public static bool IsQqMusic(string appUserModelId) =>
        appUserModelId.Contains("QQMusic", StringComparison.OrdinalIgnoreCase)
        || appUserModelId.Contains("QQ音乐", StringComparison.OrdinalIgnoreCase);

    private static async Task<MediaPlaybackInfo?> ReadSessionAsync(
        GlobalSystemMediaTransportControlsSession session,
        string appId)
    {
        var properties = await session.TryGetMediaPropertiesAsync().AsTask().ConfigureAwait(false);
        if (properties is null)
            return null;

        var title = string.IsNullOrWhiteSpace(properties.Title) ? "未知歌曲" : properties.Title.Trim();
        var artist = string.IsNullOrWhiteSpace(properties.Artist) ? "未知歌手" : properties.Artist.Trim();
        var timeline = session.GetTimelineProperties();
        var playback = session.GetPlaybackInfo();
        var isPlaying = playback.PlaybackStatus is GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

        return new MediaPlaybackInfo(
            title,
            artist,
            timeline.Position,
            timeline.EndTime,
            isPlaying,
            appId);
    }
}
