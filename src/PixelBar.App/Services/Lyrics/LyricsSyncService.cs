using PixelBar_App.Services;



namespace PixelBar_App.Services.Lyrics;



public sealed class LyricsSyncService

{

    public static LyricsSyncService Instance { get; } = new();



    private readonly MediaSessionMonitor _monitor = new();

    private readonly QqMusicLyricProvider _qqLyricProvider = new();

    private readonly object _gate = new();



    private CancellationTokenSource? _cts;

    private Task? _loopTask;

    private int _resyncVersion;

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

            UpdateStatus(LyricsSyncStatus.WaitingForQqMusic, LyricSource.None, null, null, null, BuildDiagnostic(null, null));

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

    /// <summary>清空歌词索引并强制下一轮重新匹配 QQ 音乐与 qrc 缓存。</summary>
    public void RequestResync()
    {
        _qqLyricProvider.InvalidateIndex();
        Interlocked.Increment(ref _resyncVersion);
        if (AppSettingsService.Instance.Current.LyricsEnabled)
            Start();
    }



    private async Task RunLoopAsync(CancellationToken cancellationToken)

    {

        string? trackKey = null;

        LrcDocument? document = null;

        LyricSource documentSource = LyricSource.None;

        string? lastSentLine = null;

        var consumedResyncVersion = Volatile.Read(ref _resyncVersion);



        while (!cancellationToken.IsCancellationRequested)

        {

            try

            {

                var settings = AppSettingsService.Instance.Current;

                var resyncVersion = Volatile.Read(ref _resyncVersion);

                if (resyncVersion != consumedResyncVersion)

                {

                    consumedResyncVersion = resyncVersion;

                    trackKey = null;

                    document = null;

                    documentSource = LyricSource.None;

                    lastSentLine = null;

                }

                var playback = await _monitor.TryGetQqMusicPlaybackAsync(cancellationToken).ConfigureAwait(false);

                var desktop = QqMusicDesktopLyricsReader.TryRead();

                var recentQrc = _qqLyricProvider.TryFindMostRecent(settings.QqMusicLyricDirectory);



                if (playback is null && desktop is null)

                {

                    trackKey = null;

                    document = null;

                    documentSource = LyricSource.None;

                    lastSentLine = null;

                    UpdateStatus(

                        LyricsSyncStatus.WaitingForQqMusic,

                        LyricSource.None,

                        null,

                        null,

                        null,

                        BuildDiagnostic(null, null));

                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                    continue;

                }



                var title = playback?.Title;

                var artist = playback?.Artist;



                if (string.IsNullOrWhiteSpace(title) || title == "未知歌曲")

                {

                    title = recentQrc?.Title;

                    if (string.IsNullOrWhiteSpace(title) && desktop?.Artist is not null)

                        title = GuessTitleFromRecentQrc(settings, desktop.Value.Artist) ?? LastTitle;

                }



                if (string.IsNullOrWhiteSpace(artist) || artist == "未知歌手")

                    artist = recentQrc?.Artist ?? desktop?.Artist ?? LastArtist;



                if (playback is not null && !playback.IsPlaying)

                {

                    UpdateStatus(

                        LyricsSyncStatus.Paused,

                        LastSource,

                        title,

                        artist,

                        lastSentLine,

                        BuildDiagnostic(playback, desktop));

                    await Task.Delay(800, cancellationToken).ConfigureAwait(false);

                    continue;

                }



                var nextTrackKey = $"{title}|{artist}|{recentQrc?.Title}|{recentQrc?.Artist}|{desktop?.RawTitle}";

                if (!string.Equals(trackKey, nextTrackKey, StringComparison.Ordinal))

                {

                    trackKey = nextTrackKey;

                    document = ResolveDocument(settings, title, artist, out documentSource);

                    lastSentLine = null;

                    title ??= document?.Title;

                    artist ??= document?.Artist;

                }



                var position = playback?.Position ?? TimeSpan.Zero;

                LyricSource source;

                string displayText;



                if (document?.GetLineAt(position, settings.LyricsTimingOffsetMs) is { } timedLine)

                {

                    source = documentSource;

                    displayText = timedLine;

                }

                else if (desktop is { Line: var desktopLine, Artist: var desktopArtist } && !string.IsNullOrWhiteSpace(desktopLine))

                {

                    source = LyricSource.Desktop;

                    displayText = desktopLine;

                    title ??= document?.Title;

                    artist ??= desktopArtist ?? document?.Artist;

                }

                else

                {

                    source = LyricSource.Fallback;

                    displayText = !string.IsNullOrWhiteSpace(title)

                        ? $"{title} · {artist}".Trim(' ', '·')

                        : "未找到歌词";

                }



                var scrollDirection = settings.LyricsScrollRightToLeft
                    ? PixelBar.Sdk.Protocol.TextScrollDirection.RightToLeft
                    : PixelBar.Sdk.Protocol.TextScrollDirection.LeftToRight;
                var displaySignature = $"{displayText}|{settings.LyricsScrollLongLines}|{(int)scrollDirection}";

                if (!string.Equals(lastSentLine, displaySignature, StringComparison.Ordinal))

                {

                    if (PixelBarService.Instance.HasSelectedDevice)

                    {

                        var client = PixelBarService.Instance.CreateClient();

                        LyricsDisplayFormatter.Show(
                            client,
                            displayText,
                            settings.LyricsScrollLongLines,
                            scrollDirection);

                        lastSentLine = displaySignature;

                    }



                    UpdateStatus(

                        source == LyricSource.Fallback ? LyricsSyncStatus.PlayingFallback : LyricsSyncStatus.PlayingWithLyrics,

                        source,

                        title,

                        artist,

                        displayText,

                        BuildDiagnostic(playback, desktop));

                }

                else

                {

                    UpdateStatus(

                        source == LyricSource.Fallback ? LyricsSyncStatus.PlayingFallback : LyricsSyncStatus.PlayingWithLyrics,

                        source,

                        title,

                        artist,

                        displayText,

                        BuildDiagnostic(playback, desktop));

                }

            }

            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)

            {

                break;

            }

            catch (Exception ex)

            {

                UpdateStatus(LyricsSyncStatus.Error, LyricSource.None, null, null, ex.Message, ex.Message);

            }



            await Task.Delay(350, cancellationToken).ConfigureAwait(false);

        }

    }



    private LrcDocument? ResolveDocument(AppSettings settings, string? title, string? artist, out LyricSource source)

    {

        var qq = !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(artist)

            ? _qqLyricProvider.TryFindLyrics(title ?? string.Empty, artist ?? string.Empty, settings.QqMusicLyricDirectory)

            : _qqLyricProvider.TryFindMostRecent(settings.QqMusicLyricDirectory);

        if (qq is not null && qq.Lines.Count > 0)

        {

            source = LyricSource.QrcCache;

            return qq;

        }



        source = LyricSource.None;

        return null;

    }



    private string? GuessTitleFromRecentQrc(AppSettings settings, string artistHint)

    {

        var recent = _qqLyricProvider.TryFindMostRecent(settings.QqMusicLyricDirectory);

        if (recent?.Artist is not null

            && (recent.Artist.Contains(artistHint, StringComparison.OrdinalIgnoreCase)

                || artistHint.Contains(recent.Artist, StringComparison.OrdinalIgnoreCase)))

            return recent.Title;



        return recent?.Title;

    }



    private string BuildDiagnostic(MediaPlaybackInfo? playback, DesktopLyricSnapshot? desktop)

    {

        var settings = AppSettingsService.Instance.Current;

        var qqDirs = _qqLyricProvider.GetSearchDirectories(settings.QqMusicLyricDirectory);

        var qqText = qqDirs.Count == 0 ? "未找到 QQ 音乐缓存" : string.Join("；", qqDirs);

        var desktopText = desktop is null ? "未检测到" : "已连接";

        return $"QQ qrc {_qqLyricProvider.IndexedFileCount} 首（{qqText}）；解密缓存 {_qqLyricProvider.DecryptedCacheFileCount} 个（{_qqLyricProvider.DecryptedCacheDirectory}）；桌面歌词：{desktopText}";

    }



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

    Fallback,

}



public enum LyricsSyncStatus

{

    Idle,

    WaitingForQqMusic,

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

