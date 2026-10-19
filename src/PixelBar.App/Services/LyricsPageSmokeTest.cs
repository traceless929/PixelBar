using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PixelBar_App.Pages;
using PixelBar_App.Services.Lyrics;
using System.Text.Json;

namespace PixelBar_App.Services;

/// <summary>通过命令行显式运行的 WinUI 页面回归检查，不启动歌词推送或托盘。</summary>
internal static class LyricsPageSmokeTest
{
    public static bool IsRequested => Environment.GetCommandLineArgs().Contains("--self-test-lyrics");

    private static readonly string LogPath = Environment.GetEnvironmentVariable("PIXELBAR_SELF_TEST_LOG")
        ?? Path.Combine(Path.GetTempPath(), "PixelBar-LyricsPage-self-test.log");

    private static Window? _window;
    private static bool _failed;

    public static void Write(string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(LogPath))!);
        File.AppendAllText(LogPath, $"{DateTimeOffset.Now:O} {text}{Environment.NewLine}");
    }

    public static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        _failed = true;
        Write($"FAIL unhandled: {args.Message}{Environment.NewLine}{args.Exception}");
        args.Handled = true;
        Environment.ExitCode = 1;
        Application.Current.Exit();
    }

    public static async void Run()
    {
        try
        {
            Write("START LyricsPage constructor and Loaded regression");
            await CheckTimelineAsync();
            await ClockSyncSmokeTest.RunAsync();
            var settingsBefore = JsonSerializer.Serialize(AppSettingsService.Instance.Current);
            _window = new Window();
            for (var iteration = 0; iteration < 2; iteration++)
            {
                Write($"Construct page {iteration + 1}");
                var page = new LyricsPage();
                Write("Constructor OK");
                var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                page.Loaded += (_, _) => loaded.TrySetResult();
                _window.Content = page;
                _window.Activate();
                await loaded.Task.WaitAsync(TimeSpan.FromSeconds(10));
                await Task.Delay(250);
                if (_failed)
                    return;
                Write("Loaded OK");
                _window.Content = new Grid();
                await Task.Delay(100);
            }

            var frame = new Frame();
            frame.NavigationFailed += (_, args) =>
            {
                Write($"FAIL Frame.Navigate: {args.Exception}");
                args.Handled = true;
                _failed = true;
            };
            _window.Content = frame;
            for (var iteration = 0; iteration < 2; iteration++)
            {
                Write($"Frame.Navigate LyricsPage {iteration + 1}");
                var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void OnNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs args)
                {
                    ((Page)args.Content).Loaded += (_, _) => loaded.TrySetResult();
                }
                frame.Navigated += OnNavigated;
                if (!frame.Navigate(typeof(LyricsPage)) || _failed)
                    throw new InvalidOperationException("LyricsPage navigation failed.");
                frame.Navigated -= OnNavigated;
                await loaded.Task.WaitAsync(TimeSpan.FromSeconds(10));
                await Task.Delay(250);
                if (_failed)
                    return;
                Write("Frame.Navigate and Loaded OK");
                frame.Content = new Grid();
                await Task.Delay(100);
            }

            if (JsonSerializer.Serialize(AppSettingsService.Instance.Current) != settingsBefore)
                throw new InvalidOperationException("Opening LyricsPage changed saved preferences.");
            for (var iteration = 0; iteration < 2; iteration++)
            {
                var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void OnNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs args) =>
                    ((Page)args.Content).Loaded += (_, _) => loaded.TrySetResult();
                frame.Navigated += OnNavigated;
                if (!frame.Navigate(typeof(ClockPage)) || _failed)
                    throw new InvalidOperationException("ClockPage navigation failed.");
                frame.Navigated -= OnNavigated;
                await loaded.Task.WaitAsync(TimeSpan.FromSeconds(10));
                await Task.Delay(100);
                frame.Content = new Grid();
            }
            if (JsonSerializer.Serialize(AppSettingsService.Instance.Current) != settingsBefore)
                throw new InvalidOperationException("Opening ClockPage changed saved preferences.");
            Write("PASS ClockPage navigation and Loaded, twice; preferences unchanged");
            Write("PASS direct construction and Frame.Navigate, twice each; preferences unchanged");
            Environment.ExitCode = 0;
        }
        catch (Exception error)
        {
            Write($"FAIL: {error}");
            Environment.ExitCode = 1;
        }
        finally
        {
            _window?.Close();
            Application.Current.Exit();
        }
    }

    private static async Task CheckTimelineAsync()
    {
        var document = new LrcDocument
        {
            Title = "时间轴测试",
            Lines =
            [
                new LyricLine(TimeSpan.FromSeconds(1), "测试开场"),
                new LyricLine(TimeSpan.FromSeconds(4), "测试第二行"),
                new LyricLine(TimeSpan.FromSeconds(8), "测试第三行"),
            ],
        };
        var staleDesktop = new DesktopLyricSnapshot("测试开场", null, "测试开场");
        var playback = new MediaPlaybackInfo("时间轴测试", "测试歌手", TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(20), true, "cloudmusic.exe", HasTrustedTimeline: true,
            NetEaseSongId: "12345", IsInfLinkSession: true);
        var checks = 0;
        void CheckLine(LyricsMusicProvider provider, double seconds, int offset, bool trusted,
            DesktopLyricSnapshot? desktop, string expected)
        {
            LyricsSyncService.ResolveDisplayText(provider, document, LyricSource.NetEaseApi,
                playback with { Position = TimeSpan.FromSeconds(seconds), HasTrustedTimeline = trusted },
                TimeSpan.FromSeconds(seconds), offset, desktop, document.Title, "测试歌手",
                out _, out var actual);
            if (actual != expected)
                throw new InvalidOperationException($"Timeline regression: provider={provider}, position={seconds}, trusted={trusted}; expected={expected}, actual={actual}");
            checks++;
        }
        foreach (var provider in Enum.GetValues<LyricsMusicProvider>())
        {
            CheckLine(provider, 1.5, 0, true, staleDesktop, "测试开场");
            CheckLine(provider, 4.5, 0, true, staleDesktop, "测试第二行");
            CheckLine(provider, 8.5, 0, true, staleDesktop, "测试第三行");
            CheckLine(provider, 2.2, -1900, true, staleDesktop, "测试第二行");
            CheckLine(provider, 1.5, 0, true, null, "测试开场");
        }
        CheckLine(LyricsMusicProvider.NetEaseCloudMusic, 8.5, 0, false, staleDesktop, "测试开场");

        var tracker = new NetEaseAnchoredPlaybackTracker();
        var reported = playback with { Position = TimeSpan.FromSeconds(13.4) };
        var first = tracker.ResolvePosition(reported, null, document, "id:12345");
        await Task.Delay(300);
        var next = tracker.ResolvePosition(reported, null, new LrcDocument { Lines = document.Lines }, "id:12345");
        if (next - first < TimeSpan.FromMilliseconds(150))
            throw new InvalidOperationException("SMTC position did not advance between reports.");
        if (tracker.ResolvePosition(playback, null, document, "id:12345") != playback.Position)
            throw new InvalidOperationException("Seeking backwards did not reset the SMTC anchor.");
        checks += 2;
        Write($"PASS {checks} timeline checks: stale desktop, successive lines, timing offset, extrapolation and seek");

        const string emptyVersionOne = """{"code":200,"lrc":{"version":1,"lyric":""}}""";
        const string fullVersionOne = """{"code":200,"lrc":{"version":1,"lyric":"[00:01.00]接口测试开场\n[00:04.00]接口测试第二行"}}""";
        if (NetEaseLyricApiClient.TryParseResponse(emptyVersionOne, "接口测试", "歌手") is not null)
            throw new InvalidOperationException("An empty version-1 response must not be cached as valid lyrics.");
        var apiDocument = NetEaseLyricApiClient.TryParseResponse(fullVersionOne, "接口测试", "歌手");
        if (apiDocument?.Lines.Count != 2 || apiDocument.GetSingableLineAt(TimeSpan.FromSeconds(4.5)) != "接口测试第二行")
            throw new InvalidOperationException("Full version-1 lyrics did not advance to the second line.");
        Write("PASS 2 NetEase API response checks: empty version-1 response rejected, full response advances");
    }
}
