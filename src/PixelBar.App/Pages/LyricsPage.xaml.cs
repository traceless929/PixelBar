using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PixelBar_App.Helpers;
using PixelBar_App.Services;
using PixelBar_App.Services.Lyrics;
using Windows.System;

namespace PixelBar_App.Pages;

public sealed partial class LyricsPage : Page
{
    private bool _loaded;
    private bool _suppressSave;

    public LyricsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _loaded = true;
        var settings = AppSettingsService.Instance.Current;
        _suppressSave = true;
        EnableSwitch.IsOn = settings.LyricsEnabled;
        ScrollSwitch.IsOn = settings.LyricsScrollLongLines;
        TimingOffsetBox.Value = settings.LyricsTimingOffsetMs;
        SelectScrollDirection(settings.LyricsScrollRightToLeft);
        UpdateScrollDirectionPanelVisibility();
        LyricDirBox.Text = settings.QqMusicLyricDirectory
            ?? QqMusicCacheLocator.GetPrimaryCachePath()
            ?? string.Empty;
        _suppressSave = false;

        LyricsSyncService.Instance.StatusChanged += OnLyricsStatusChanged;
        RefreshStatus(
            LyricsSyncService.Instance.CurrentStatus,
            LyricsSyncService.Instance.LastSource,
            LyricsSyncService.Instance.LastTitle,
            LyricsSyncService.Instance.LastArtist,
            LyricsSyncService.Instance.LastLine,
            LyricsSyncService.Instance.DiagnosticMessage);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) =>
        LyricsSyncService.Instance.StatusChanged -= OnLyricsStatusChanged;

    private void EnableSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loaded || _suppressSave)
            return;

        SaveSettings();
        if (EnableSwitch.IsOn && !PixelBarService.Instance.HasSelectedDevice)
            UiFeedback.Show(StatusBar, InfoBarSeverity.Warning, "尚未选择 PixelBar 设备，请先在设置中连接。");
    }

    private void ScrollSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loaded || _suppressSave)
            return;

        UpdateScrollDirectionPanelVisibility();
        SaveSettings();
    }

    private void TimingOffsetBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_loaded || _suppressSave || double.IsNaN(args.NewValue))
            return;

        SaveSettings();
    }

    private void ScrollDirectionGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded || _suppressSave)
            return;

        SaveSettings();
    }

    private void LyricDirBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loaded || _suppressSave)
            return;

        SaveSettings();
    }

    private void SaveSettings()
    {
        var offsetMs = (int)Math.Clamp(Math.Round(TimingOffsetBox.Value), -30_000, 30_000);
        AppSettingsService.Instance.UpdateLyricsSettings(
            EnableSwitch.IsOn,
            ScrollSwitch.IsOn,
            offsetMs,
            GetSelectedScrollRightToLeft(),
            LyricDirBox.Text);
    }

    private void SelectScrollDirection(bool rightToLeft)
    {
        foreach (var item in ScrollDirectionGroup.Items)
        {
            if (item is RadioButton radio && radio.Tag is string tag)
                radio.IsChecked = rightToLeft ? tag == "rtl" : tag == "ltr";
        }
    }

    private bool GetSelectedScrollRightToLeft()
    {
        foreach (var item in ScrollDirectionGroup.Items)
        {
            if (item is RadioButton { IsChecked: true, Tag: string tag })
                return tag == "rtl";
        }

        return false;
    }

    private void UpdateScrollDirectionPanelVisibility() =>
        ScrollDirectionPanel.Visibility = ScrollSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;

    private async void OpenWikiButton_Click(object sender, RoutedEventArgs e) =>
        await Launcher.LaunchUriAsync(new Uri("https://github.com/traceless929/PixelBar/wiki/Lyrics-QQMusic"));

    private void OnLyricsStatusChanged(object? sender, LyricsSyncStatusEvent e) =>
        DispatcherQueue.TryEnqueue(() => RefreshStatus(
            e.Status,
            e.Source,
            e.Title,
            e.Artist,
            e.Line,
            e.Diagnostic));

    private void RefreshStatus(
        LyricsSyncStatus status,
        LyricSource source,
        string? title,
        string? artist,
        string? line,
        string? diagnostic)
    {
        StateText.Text = status switch
        {
            LyricsSyncStatus.Idle => "未启用",
            LyricsSyncStatus.WaitingForQqMusic => "等待 QQ 音乐播放…",
            LyricsSyncStatus.Paused => "QQ 音乐已暂停",
            LyricsSyncStatus.PlayingWithLyrics => "正在推送歌词",
            LyricsSyncStatus.PlayingFallback => "正在推送（未匹配到歌词，显示歌名）",
            LyricsSyncStatus.Error => "出错",
            _ => status.ToString(),
        };

        SongText.Text = title is null ? "—" : $"{title} · {artist}";
        LineText.Text = string.IsNullOrWhiteSpace(line) ? "—" : line;
        SourceText.Text = source switch
        {
            LyricSource.Desktop => "来源：QQ 音乐桌面歌词",
            LyricSource.QrcCache => "来源：QQ 音乐 qrc 缓存",
            LyricSource.Fallback => "来源：回退（歌名/歌手）",
            _ => "来源：—",
        };
        DiagnosticText.Text = string.IsNullOrWhiteSpace(diagnostic) ? "—" : diagnostic;
    }
}
