using PixelBar_App.Services;

namespace PixelBar_App.Services.Lyrics;

public sealed class LyricsSyncService
{
    public static LyricsSyncService Instance { get; } = new();

    private readonly MediaSessionMonitor _monitor = new();
    private readonly QqMusicLyricProvider _qqLyricProvider = new();
    private readonly NetEaseLyricProvider _netEaseLyricProvider = new();
    private readonly NetEaseAnchoredPlaybackTracker _netEasePositionTracker = new();
    private readonly object _gate = new();
    private readonly SemaphoreSlim _displayGate = new(1, 1);
    private int _previewActive;
    private int _displayVersion;

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private int _resyncVersion;
    private string? _activeTrackKey;
    private TimeSpan _lastPlaybackDuration;
    private TimeSpan _lastPlaybackPosition;
    private string? _loadedLookupTitle;
    private string? _loadedLookupArtist;

    public event EventHandler<LyricsSyncStatusEvent>? StatusChanged;

    public LyricsSyncStatus CurrentStatus { get; private set; } = LyricsSyncStatus.Idle;
    public LyricSource LastSource { get; private set; } = LyricSource.None;
    public string? LastTitle { get; private set; }
    public string? LastArtist { get; private set; }
    public string? LastLine { get; private set; }
    public string? DiagnosticMessage { get; private set; }

    public void ApplySettings()
    {
        var settings = AppSettingsService.Instance.Current;
        if (settings.LyricsEnabled)
            Start();
        else
            Stop();
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_loopTask is { IsCompleted: false })
                return;

            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
            UpdateStatus(
                LyricsSyncStatus.WaitingForPlayer,
                LyricSource.None,
                null,
                null,
                null,
                BuildDiagnostic(AppSettingsService.Instance.Current.LyricsProvider, null, null));
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _loopTask = null;
            UpdateStatus(LyricsSyncStatus.Idle, LyricSource.None, null, null, null, null);
        }
    }

    /// <summary>清空歌词索引并强制下一轮重新匹配播放器与本地缓存。</summary>
    public void RequestResync()
    {
        var provider = AppSettingsService.Instance.Current.LyricsProvider;
        if (provider == LyricsMusicProvider.QqMusic)
            _qqLyricProvider.InvalidateIndex();
        else
            _netEaseLyricProvider.InvalidateIndex();

        _netEasePositionTracker.Reset();
        Interlocked.Increment(ref _resyncVersion);
        if (AppSettingsService.Instance.Current.LyricsEnabled)
            Start();
    }

    /// <summary>串行试播三行歌词；预览期间暂停正常推送，结束后恢复当前歌词。</summary>
    public async Task PreviewAnimationAsync(PixelBar.Sdk.Protocol.LyricAnimation animation, CancellationToken cancellationToken)
    {
        if (!PixelBarService.Instance.HasSelectedDevice)
            throw new InvalidOperationException("请先在「设置」中连接 PixelBar。");
        if (Interlocked.CompareExchange(ref _previewActive, 1, 0) != 0)
            throw new InvalidOperationException("歌词动画正在试播，请稍候。");

        try
        {
            await _displayGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var client = PixelBarService.Instance.CreateClient();
                foreach (var line in new[] { "歌词动画预览", "下一句缓缓入场", "让音乐动起来" })
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    LyricsDisplayFormatter.Show(client, line, false, animation: animation);
                    await Task.Delay(1600, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _displayGate.Release();
            }
        }
        finally
        {
            Interlocked.Increment(ref _displayVersion);
            Volatile.Write(ref _previewActive, 0);
        }
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        string? trackKey = null;
        LrcDocument? document = null;
        LyricSource documentSource = LyricSource.None;
        string? lastSentLine = null;
        var nextDocumentRetryUtc = DateTime.MinValue;
        var consumedResyncVersion = Volatile.Read(ref _resyncVersion);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (Volatile.Read(ref _previewActive) != 0)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var settings = AppSettingsService.Instance.Current;
                var provider = settings.LyricsProvider;
                var resyncVersion = Volatile.Read(ref _resyncVersion);
                if (resyncVersion != consumedResyncVersion)
                {
                    consumedResyncVersion = resyncVersion;
                    trackKey = null;
                    document = null;
                    documentSource = LyricSource.None;
                    lastSentLine = null;
                    _activeTrackKey = null;
                    _loadedLookupTitle = null;
                    _loadedLookupArtist = null;
                    _lastPlaybackPosition = TimeSpan.Zero;
                    _netEasePositionTracker.Reset();
                }

                var playback = await _monitor.TryGetPlaybackAsync(provider, cancellationToken).ConfigureAwait(false);
                // InfLink 已提供可靠进度时，不再从整个网易云窗口猜测当前歌词。
                var desktop = provider == LyricsMusicProvider.NetEaseCloudMusic && playback?.HasTrustedTimeline == true
                    ? null
                    : TryReadDesktopLyrics(provider);
                playback ??= TryBuildFallbackPlayback(provider);

                if (playback is null && desktop is null)
                {
                    trackKey = null;
                    document = null;
                    documentSource = LyricSource.None;
                    lastSentLine = null;
                    _activeTrackKey = null;
                    _loadedLookupTitle = null;
                    _loadedLookupArtist = null;
                    _lastPlaybackPosition = TimeSpan.Zero;
                    _netEasePositionTracker.Reset();
                    UpdateStatus(
                        LyricsSyncStatus.WaitingForPlayer,
                        LyricSource.None,
                        null,
                        null,
                        null,
                        BuildDiagnostic(provider, null, null));
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var smtcTitle = NormalizeSmtcField(playback?.Title);
                var smtcArtist = NormalizeSmtcField(playback?.Artist);
                var netEaseMainWindow = provider == LyricsMusicProvider.NetEaseCloudMusic
                    ? NetEaseDesktopLyricsReader.TryParseMainWindowTrack()
                    : null;

                var lookupTitle = smtcTitle ?? netEaseMainWindow?.Title;
                var lookupArtist = smtcArtist ?? netEaseMainWindow?.Artist;

                if (provider == LyricsMusicProvider.QqMusic)
                {
                    lookupArtist ??= NormalizeSmtcField(desktop?.Artist);
                    if (lookupTitle is null
                        && desktop?.Line is { Length: > 0 } qqDesktopLine
                        && QqMusicDesktopLyricsReader.IsUsableLyricLine(qqDesktopLine))
                    {
                        lookupTitle = TryResolveQqTitleFromDesktop(
                            settings,
                            lookupArtist,
                            qqDesktopLine,
                            document);
                    }
                }

                var displayTitle = lookupTitle ?? document?.Title ?? LastTitle;
                var displayArtist = lookupArtist ?? document?.Artist ?? LastArtist;

                if (playback is not null && !playback.IsPlaying)
                {
                    UpdateStatus(
                        LyricsSyncStatus.Paused,
                        LastSource,
                        displayTitle,
                        displayArtist,
                        lastSentLine,
                        BuildDiagnostic(provider, playback, desktop));
                    await Task.Delay(800, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var nextTrackKey = BuildTrackKey(
                    provider,
                    playback?.NetEaseSongId,
                    lookupTitle,
                    lookupArtist,
                    playback?.Duration ?? TimeSpan.Zero);

                var documentMismatch = document is not null
                                       && lookupTitle is not null
                                       && !LyricMatchHelper.DocumentMatchesTrack(document, lookupTitle, lookupArtist);
                var identityChanged = _loadedLookupTitle is not null
                                      && lookupTitle is not null
                                      && !LyricMatchHelper.TrackMatches(
                                          _loadedLookupTitle,
                                          _loadedLookupArtist,
                                          lookupTitle,
                                          lookupArtist);

                var forceTrackChange = ShouldForceTrackChange(
                    provider,
                    playback,
                    document,
                    desktop,
                    trackKey,
                    nextTrackKey,
                    lookupTitle,
                    lookupArtist);

                var needsDocumentReload = forceTrackChange
                                          || documentMismatch
                                          || identityChanged
                                          || !string.Equals(trackKey, nextTrackKey, StringComparison.Ordinal)
                                          || (document is null && DateTime.UtcNow >= nextDocumentRetryUtc);

                if (needsDocumentReload)
                {
                    trackKey = nextTrackKey;
                    _activeTrackKey = nextTrackKey;
                    _lastPlaybackDuration = playback?.Duration ?? TimeSpan.Zero;
                    _lastPlaybackPosition = playback?.Position ?? TimeSpan.Zero;
                    // 重新加载同一首歌的文档不能重置进度外推；切歌由 tracker 的 trackKey 检测。
                    (document, documentSource) = await ResolveDocumentAsync(
                        provider,
                        settings,
                        lookupTitle,
                        lookupArtist,
                        playback?.NetEaseSongId,
                        cancellationToken).ConfigureAwait(false);
                    nextDocumentRetryUtc = DateTime.UtcNow.AddSeconds(10);
                    // 歌词文档可因桌面歌词匹配失败而反复重载；屏幕未变化时保留去重状态。
                    _loadedLookupTitle = lookupTitle ?? document?.Title;
                    _loadedLookupArtist = lookupArtist ?? document?.Artist;
                    displayTitle ??= document?.Title;
                    displayArtist ??= document?.Artist;

                    if (provider == LyricsMusicProvider.NetEaseCloudMusic)
                        _netEaseLyricProvider.RememberTrackMetadata(
                            displayTitle,
                            displayArtist,
                            playback?.NetEaseSongId,
                            settings.NetEaseLyricDirectory);
                }

                var title = displayTitle;
                var artist = displayArtist;

                var position = playback?.HasTrustedTimeline == true
                    || provider == LyricsMusicProvider.NetEaseCloudMusic
                    ? _netEasePositionTracker.ResolvePosition(playback, desktop, document, nextTrackKey)
                    : playback?.Position ?? TimeSpan.Zero;
                ResolveDisplayText(
                    provider,
                    document,
                    documentSource,
                    playback,
                    position,
                    settings.LyricsTimingOffsetMs,
                    desktop,
                    title,
                    artist,
                    out var source,
                    out var displayText);

                var scrollDirection = settings.LyricsScrollRightToLeft
                    ? PixelBar.Sdk.Protocol.TextScrollDirection.RightToLeft
                    : PixelBar.Sdk.Protocol.TextScrollDirection.LeftToRight;
                var animation = Enum.IsDefined(settings.LyricsAnimation)
                    ? settings.LyricsAnimation
                    : PixelBar.Sdk.Protocol.LyricAnimation.BottomToTop;
                var displaySignature = $"{nextTrackKey}|{displayText}|{settings.LyricsScrollLongLines}|{(int)scrollDirection}|{(int)animation}|{Volatile.Read(ref _displayVersion)}";

                if (!string.Equals(lastSentLine, displaySignature, StringComparison.Ordinal))
                {
                    if (PixelBarService.Instance.HasSelectedDevice)
                    {
                        try
                        {
                            await _displayGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                if (Volatile.Read(ref _previewActive) != 0)
                                    continue;

                                var client = PixelBarService.Instance.CreateClient();
                                LyricsDisplayFormatter.Show(
                                    client,
                                    displayText,
                                    settings.LyricsScrollLongLines,
                                    scrollDirection,
                                    animation);
                                lastSentLine = displaySignature;
                            }
                            finally
                            {
                                _displayGate.Release();
                            }
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            var deviceError = FormatExceptionMessage(ex);
                            UpdateStatus(
                                LyricsSyncStatus.Error,
                                source,
                                title,
                                artist,
                                deviceError,
                                $"{BuildDiagnostic(provider, playback, desktop)}；设备：{deviceError}");
                            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                            continue;
                        }
                    }

                    UpdateStatus(
                        source == LyricSource.Fallback ? LyricsSyncStatus.PlayingFallback : LyricsSyncStatus.PlayingWithLyrics,
                        source,
                        title,
                        artist,
                        displayText,
                        BuildDiagnostic(provider, playback, desktop));
                }
                else
                {
                    UpdateStatus(
                        source == LyricSource.Fallback ? LyricsSyncStatus.PlayingFallback : LyricsSyncStatus.PlayingWithLyrics,
                        source,
                        title,
                        artist,
                        displayText,
                        BuildDiagnostic(provider, playback, desktop));
                }

                if (playback?.IsPlaying == true)
                    _lastPlaybackPosition = playback.Position;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                var message = FormatExceptionMessage(ex);
                UpdateStatus(
                    LyricsSyncStatus.Error,
                    LastSource,
                    LastTitle,
                    LastArtist,
                    message,
                    $"{BuildDiagnostic(AppSettingsService.Instance.Current.LyricsProvider, null, null)}；错误：{message}");
            }

            await Task.Delay(350, cancellationToken).ConfigureAwait(false);
        }
    }

    private static DesktopLyricSnapshot? TryReadDesktopLyrics(LyricsMusicProvider provider) =>
        provider switch
        {
            LyricsMusicProvider.QqMusic => QqMusicDesktopLyricsReader.TryRead(),
            LyricsMusicProvider.NetEaseCloudMusic => NetEaseDesktopLyricsReader.TryRead(),
            _ => null,
        };

    private MediaPlaybackInfo? TryBuildFallbackPlayback(LyricsMusicProvider provider)
    {
        if (provider != LyricsMusicProvider.NetEaseCloudMusic
            || !NetEaseDesktopLyricsReader.IsProcessRunning())
        {
            return null;
        }

        var track = NetEaseDesktopLyricsReader.TryParseMainWindowTrack();
        if (track is not { Title: { Length: > 0 } title })
            return null;

        return new MediaPlaybackInfo(
            title,
            track.Value.Artist ?? "未知歌手",
            TimeSpan.Zero,
            TimeSpan.Zero,
            true,
            "cloudmusic.exe",
            HasTrustedTimeline: false);
    }

    private async Task<(LrcDocument? Document, LyricSource Source)> ResolveDocumentAsync(
        LyricsMusicProvider provider,
        AppSettings settings,
        string? title,
        string? artist,
        string? netEaseSongId,
        CancellationToken cancellationToken)
    {
        if (provider == LyricsMusicProvider.NetEaseCloudMusic)
        {
            var (netEase, source) = await _netEaseLyricProvider.TryFindLyricsForTrackAsync(
                title,
                artist,
                netEaseSongId,
                settings.NetEaseLyricDirectory,
                cancellationToken).ConfigureAwait(false);
            if (netEase is not null && netEase.Lines.Count > 0)
                return (netEase, source);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(artist))
                return (null, LyricSource.None);

            var qq = _qqLyricProvider.TryFindLyrics(
                title ?? string.Empty,
                artist ?? string.Empty,
                settings.QqMusicLyricDirectory);
            if (qq is not null && qq.Lines.Count > 0)
                return (qq, LyricSource.QrcCache);
        }

        return (null, LyricSource.None);
    }

    internal static void ResolveDisplayText(
        LyricsMusicProvider provider,
        LrcDocument? document,
        LyricSource documentSource,
        MediaPlaybackInfo? playback,
        TimeSpan position,
        int timingOffsetMs,
        DesktopLyricSnapshot? desktop,
        string? title,
        string? artist,
        out LyricSource source,
        out string displayText)
    {
        var preferTimedLine = playback?.HasTrustedTimeline == true;

        if (document is not null)
        {
            var timedLine = (playback?.HasTrustedTimeline == true || position > TimeSpan.Zero)
                ? document.GetSingableLineAt(position, timingOffsetMs)
                : null;

            if (!preferTimedLine
                && desktop?.Line is { Length: > 0 } desktopLine
                && IsUsableDesktopLine(provider, desktopLine, title, artist)
                && LyricMatchHelper.FindMatchingLine(document, desktopLine) is { } matchedLine)
            {
                if (timedLine is null || !LyricMatchHelper.LinesEquivalent(timedLine, matchedLine))
                {
                    source = documentSource;
                    displayText = matchedLine;
                    return;
                }
            }

            if (timedLine is { Length: > 0 })
            {
                source = documentSource;
                displayText = timedLine;
                return;
            }

            if (!preferTimedLine
                && desktop?.Line is { Length: > 0 } rawDesktopLine
                && IsUsableDesktopLine(provider, rawDesktopLine, title, artist))
            {
                source = documentSource;
                displayText = rawDesktopLine;
                return;
            }

            if (playback?.HasTrustedTimeline == true)
            {
                source = documentSource;
                displayText = document.GetSingableLineAt(position, timingOffsetMs)
                    ?? (!string.IsNullOrWhiteSpace(title) ? $"{title} · {artist}".Trim(' ', '·') : "…");
                return;
            }
        }

        if (desktop is { Line: var fallbackDesktopLine }
            && IsUsableDesktopLine(provider, fallbackDesktopLine, title, artist))
        {
            source = LyricSource.Desktop;
            displayText = fallbackDesktopLine;
            return;
        }

        source = LyricSource.Fallback;
        displayText = !string.IsNullOrWhiteSpace(title)
            ? $"{title} · {artist}".Trim(' ', '·')
            : "未找到歌词";
    }

    private static bool IsUsableDesktopLine(
        LyricsMusicProvider provider,
        string line,
        string? title,
        string? artist) =>
        provider switch
        {
            LyricsMusicProvider.NetEaseCloudMusic => NetEaseDesktopLyricsReader.IsUsableLyricLine(line, title, artist),
            LyricsMusicProvider.QqMusic => QqMusicDesktopLyricsReader.IsUsableLyricLine(line, title, artist),
            _ => !string.IsNullOrWhiteSpace(line),
        };

    private static string? NormalizeSmtcField(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();
        return value is "未知歌曲" or "未知歌手" ? null : value;
    }

    private static string BuildTrackKey(
        LyricsMusicProvider provider,
        string? netEaseSongId,
        string? title,
        string? artist,
        TimeSpan duration)
    {
        if (provider == LyricsMusicProvider.NetEaseCloudMusic
            && InfLinkSmtcParser.IsNumericSongId(netEaseSongId))
        {
            return $"{provider}|id:{netEaseSongId}";
        }

        var durationPart = duration > TimeSpan.FromSeconds(5)
            ? ((int)duration.TotalSeconds).ToString()
            : "0";
        return $"{provider}|{title ?? string.Empty}|{artist ?? string.Empty}|{durationPart}";
    }

    private bool ShouldForceTrackChange(
        LyricsMusicProvider provider,
        MediaPlaybackInfo? playback,
        LrcDocument? document,
        DesktopLyricSnapshot? desktop,
        string? currentTrackKey,
        string nextTrackKey,
        string? lookupTitle,
        string? lookupArtist)
    {
        if (currentTrackKey is null || document is null)
            return false;

        if (!string.Equals(currentTrackKey, nextTrackKey, StringComparison.Ordinal))
            return false;

        if (provider == LyricsMusicProvider.NetEaseCloudMusic
            && InfLinkSmtcParser.IsNumericSongId(playback?.NetEaseSongId)
            && _activeTrackKey is not null
            && !_activeTrackKey.Contains($"id:{playback!.NetEaseSongId}", StringComparison.Ordinal))
        {
            return true;
        }

        if (playback?.Duration > TimeSpan.FromSeconds(10)
            && _lastPlaybackDuration > TimeSpan.FromSeconds(10)
            && Math.Abs((playback.Duration - _lastPlaybackDuration).TotalSeconds) > 8)
        {
            return true;
        }

        if (provider == LyricsMusicProvider.QqMusic
            && lookupTitle is not null
            && document is not null
            && !LyricMatchHelper.DocumentMatchesTrack(document, lookupTitle, lookupArtist))
        {
            return true;
        }

        if (playback?.IsPlaying == true)
        {
            var pos = playback.Position;
            if (_lastPlaybackPosition > TimeSpan.FromSeconds(15)
                && pos < TimeSpan.FromSeconds(10)
                && pos + TimeSpan.FromSeconds(10) < _lastPlaybackPosition)
            {
                return true;
            }
        }

        if (desktop?.Line is { Length: > 0 } desktopLine
            && IsUsableDesktopLine(provider, desktopLine, lookupTitle, lookupArtist)
            && LyricMatchHelper.FindMatchingLine(document, desktopLine) is null
            && !LyricMetadataFilter.IsMetadataLine(desktopLine))
        {
            return true;
        }

        return false;
    }

    private string? TryResolveQqTitleFromDesktop(
        AppSettings settings,
        string? artistHint,
        string desktopLine,
        LrcDocument? currentDocument)
    {
        if (currentDocument is not null
            && LyricMatchHelper.FindMatchingLine(currentDocument, desktopLine) is not null
            && currentDocument.Title is not null)
        {
            return currentDocument.Title;
        }

        return _qqLyricProvider.TryFindTitleByDesktopLine(
            desktopLine,
            artistHint,
            settings.QqMusicLyricDirectory);
    }

    private string BuildDiagnostic(
        LyricsMusicProvider provider,
        MediaPlaybackInfo? playback,
        DesktopLyricSnapshot? desktop)
    {
        var settings = AppSettingsService.Instance.Current;
        var desktopText = desktop is { Line: var desktopLine }
                          && NetEaseDesktopLyricsReader.IsUsableLyricLine(desktopLine)
            ? "已连接"
            : NetEaseDesktopLyricsReader.IsDesktopLyricWindowOpen()
                ? "已连接（窗口已开，等待歌词换行）"
                : "未检测到";

        if (provider == LyricsMusicProvider.NetEaseCloudMusic)
        {
            var dirs = _netEaseLyricProvider.GetSearchDirectories(settings.NetEaseLyricDirectory);
            var dirText = dirs.Count == 0 ? "未找到网易云歌词缓存" : string.Join("；", dirs);
            var smtcText = playback switch
            {
                null => "未检测到",
                { IsInfLinkSession: true, NetEaseSongId: var id, HasTrustedTimeline: true, Position: var pos }
                    => $"InfLink-rs（ID {id}，进度 {FormatPosition(pos)}）",
                { IsInfLinkSession: true, NetEaseSongId: var id } => $"InfLink-rs 已连接（ID {id}）",
                { HasTrustedTimeline: true, Position: var pos } => $"已连接（系统进度 {FormatPosition(pos)}）",
                { IsPlaying: true, Duration: var d } when d > TimeSpan.FromSeconds(5)
                    => "SMTC 已连接但进度不可用，桌面锚定 + 内置计时",
                _ => _netEasePositionTracker.LastMode == NetEasePositionMode.AnchoredTimer
                    ? "无 InfLink-rs，请先一键安装"
                    : "缺少 InfLink-rs（必需）",
            };
            return $"网易云缓存 {_netEaseLyricProvider.IndexedFileCount} 首（{dirText}）；SMTC：{smtcText}；桌面歌词：{desktopText}";
        }

        if (provider == LyricsMusicProvider.QqMusic)
            desktopText = desktop is null ? "未检测到" : "已连接";

        var qqDirs = _qqLyricProvider.GetSearchDirectories(settings.QqMusicLyricDirectory);
        var qqText = qqDirs.Count == 0 ? "未找到 QQ 音乐缓存" : string.Join("；", qqDirs);
        return $"QQ qrc {_qqLyricProvider.IndexedFileCount} 首（{qqText}）；解密缓存 {_qqLyricProvider.DecryptedCacheFileCount} 个（{_qqLyricProvider.DecryptedCacheDirectory}）；桌面歌词：{desktopText}";
    }

    private static string FormatPosition(TimeSpan position) =>
        position.TotalMinutes >= 1
            ? position.ToString(@"m\:ss")
            : $"{position.TotalSeconds:F1}s";

    private static string FormatExceptionMessage(Exception ex) =>
        string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message;

    private void UpdateStatus(
        LyricsSyncStatus status,
        LyricSource source,
        string? title,
        string? artist,
        string? line,
        string? diagnostic)
    {
        CurrentStatus = status;
        LastSource = source;
        if (title is not null)
            LastTitle = title;
        if (artist is not null)
            LastArtist = artist;
        if (line is not null)
            LastLine = line;
        if (diagnostic is not null)
            DiagnosticMessage = diagnostic;

        StatusChanged?.Invoke(this, new LyricsSyncStatusEvent(
            title ?? LastTitle,
            artist ?? LastArtist,
            line ?? LastLine,
            status,
            source,
            DiagnosticMessage));
    }
}

public enum LyricSource
{
    None,
    Desktop,
    QrcCache,
    NetEaseCache,
    NetEaseApi,
    Fallback,
}

public enum LyricsSyncStatus
{
    Idle,
    WaitingForPlayer,
    Paused,
    PlayingWithLyrics,
    PlayingFallback,
    Error,
}

public sealed class LyricsSyncStatusEvent(
    string? title,
    string? artist,
    string? line,
    LyricsSyncStatus status,
    LyricSource source,
    string? diagnostic)
    : EventArgs
{
    public string? Title { get; } = title;
    public string? Artist { get; } = artist;
    public string? Line { get; } = line;
    public LyricsSyncStatus Status { get; } = status;
    public LyricSource Source { get; } = source;
    public string? Diagnostic { get; } = diagnostic;
}
