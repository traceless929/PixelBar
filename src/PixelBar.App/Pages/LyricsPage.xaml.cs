using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PixelBar_App.Helpers;
using PixelBar_App.Services;
using PixelBar_App.Services.Lyrics;
using PixelBar.Sdk.Protocol;
using Windows.System;

namespace PixelBar_App.Pages;

public sealed partial class LyricsPage : Page
{
    private bool _loaded;
    private bool _suppressSave;
    private LyricsMusicProvider _selectedProvider = LyricsMusicProvider.QqMusic;
    private CancellationTokenSource? _previewCancellation;

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
        SelectAnimation(settings.LyricsAnimation);
        SelectProvider(settings.LyricsProvider);
        UpdateScrollDirectionPanelVisibility();
        UpdateAnimationUi();
        UpdateProviderUi();
        UpdateInfLinkUi();
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

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _loaded = false;
        _previewCancellation?.Cancel();
        LyricsSyncService.Instance.StatusChanged -= OnLyricsStatusChanged;
    }

    private void EnableSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_loaded || _suppressSave)
            return;

        SaveSettings();
        UpdateAnimationUi();
        if (EnableSwitch.IsOn && _selectedProvider == LyricsMusicProvider.NetEaseCloudMusic
            && NetEaseInfLinkInstaller.GetInstallState() != NetEaseInfLinkInstallState.Installed)
        {
            UiFeedback.Show(StatusBar, InfoBarSeverity.Warning, "尚未检测到 InfLink-rs，请先点击下方「一键安装 InfLink-rs」并重启网易云。");
        }
        else if (EnableSwitch.IsOn && !PixelBarService.Instance.HasSelectedDevice)
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

    private void ProviderGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded || _suppressSave)
            return;

        _selectedProvider = GetSelectedProvider();
        UpdateProviderUi();
        UpdateInfLinkUi();
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
        var settings = AppSettingsService.Instance.Current;
        AppSettingsService.Instance.UpdateLyricsSettings(
            EnableSwitch.IsOn,
            ScrollSwitch.IsOn,
            offsetMs,
            GetSelectedScrollRightToLeft(),
            _selectedProvider,
            _selectedProvider == LyricsMusicProvider.QqMusic ? LyricDirBox.Text : settings.QqMusicLyricDirectory,
            _selectedProvider == LyricsMusicProvider.NetEaseCloudMusic ? LyricDirBox.Text : settings.NetEaseLyricDirectory,
            GetSelectedAnimation());
    }

    private void AnimationGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loaded || _suppressSave)
            return;

        UpdateAnimationUi();
        SaveSettings();
    }

    private void SelectAnimation(LyricAnimation animation)
    {
        var selected = Enum.IsDefined(animation) ? animation : LyricAnimation.BottomToTop;
        foreach (var item in AnimationGroup.Items)
        {
            if (item is RadioButton radio && byte.TryParse(radio.Tag as string, out var value))
                radio.IsChecked = value == (byte)selected;
        }
    }

    private LyricAnimation GetSelectedAnimation()
    {
        foreach (var item in AnimationGroup.Items)
        {
            if (item is RadioButton { IsChecked: true, Tag: string tag }
                && byte.TryParse(tag, out var value) && Enum.IsDefined((LyricAnimation)value))
                return (LyricAnimation)value;
        }

        return LyricAnimation.BottomToTop;
    }

    private void UpdateAnimationUi()
    {
        var allowScroll = EnableSwitch.IsOn && GetSelectedAnimation() == LyricAnimation.None;
        ScrollSwitch.IsEnabled = allowScroll;
        ScrollDirectionGroup.IsEnabled = allowScroll;
        UpdateScrollDirectionPanelVisibility();
    }

    private async void PreviewAnimationButton_Click(object sender, RoutedEventArgs e)
    {
        if (_previewCancellation is not null)
            return;

        using var cancellation = new CancellationTokenSource();
        _previewCancellation = cancellation;
        PreviewAnimationButton.IsEnabled = false;
        AnimationGroup.IsEnabled = false;
        UiFeedback.Show(StatusBar, InfoBarSeverity.Informational, "正在试播，请观察音响像素屏上的三行文字…");
        try
        {
            await LyricsSyncService.Instance.PreviewAnimationAsync(GetSelectedAnimation(), cancellation.Token);
            if (_loaded)
                UiFeedback.Show(StatusBar, InfoBarSeverity.Success, "试播完成；已恢复正常歌词同步。");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (_loaded)
                UiFeedback.Show(StatusBar, InfoBarSeverity.Error, ex.Message);
        }
        finally
        {
            _previewCancellation = null;
            PreviewAnimationButton.IsEnabled = true;
            AnimationGroup.IsEnabled = true;
        }
    }

    private void SelectProvider(LyricsMusicProvider provider)
    {
        _selectedProvider = provider;
        foreach (var item in ProviderGroup.Items)
        {
            if (item is RadioButton radio && radio.Tag is string tag)
            {
                radio.IsChecked = provider switch
                {
                    LyricsMusicProvider.NetEaseCloudMusic => tag == "netease",
                    _ => tag == "qq",
                };
            }
        }
    }

    private LyricsMusicProvider GetSelectedProvider()
    {
        foreach (var item in ProviderGroup.Items)
        {
            if (item is RadioButton { IsChecked: true, Tag: string tag })
                return tag == "netease" ? LyricsMusicProvider.NetEaseCloudMusic : LyricsMusicProvider.QqMusic;
        }

        return LyricsMusicProvider.QqMusic;
    }

    private void UpdateProviderUi()
    {
        var isQq = _selectedProvider == LyricsMusicProvider.QqMusic;
        var settings = AppSettingsService.Instance.Current;

        ProviderHintText.Text = isQq
            ? "读取 QQ 音乐本地 qrc 缓存，并通过 Windows 媒体控件同步进度。"
            : "读取网易云 PC 版歌词缓存；须先安装 BetterNCM + apoint123/InfLink-rs 以获取播放进度（见下方一键安装）。";

        EnableHintText.Text = isQq
            ? "开启后后台约每秒检测 QQ 音乐；先开 PixelBar 再开 QQ 音乐也会自动接上，无需重启应用。"
            : "开启后后台约每秒检测网易云；诊断中 SMTC 须显示「InfLink-rs（进度 …）」。";

        AdvancedHintText.Text = isQq
            ? "QQ 音乐会在 QQMusicLyricNew 写入加密 qrc；PixelBar 解密后缓存至 %LocalAppData%\\PixelBar\\LyricCache。"
            : "网易云新版会在 Temp 目录写入 API 歌词缓存（hash 文件名）；旧版为 webdata\\lyric。PixelBar 解析 YRC/LRC 后用于同步。";

        LyricDirBox.Header = isQq ? "QQ 音乐缓存根目录" : "网易云歌词缓存目录";
        LyricDirBox.Text = isQq
            ? settings.QqMusicLyricDirectory ?? QqMusicCacheLocator.GetPrimaryCachePath() ?? string.Empty
            : settings.NetEaseLyricDirectory ?? NetEaseCloudMusicCacheLocator.GetPrimaryCachePath() ?? string.Empty;

        GuidePanel.Children.Clear();
        foreach (var step in BuildGuideSteps(isQq))
            GuidePanel.Children.Add(step);
    }

    private void UpdateInfLinkUi()
    {
        var isNetEase = _selectedProvider == LyricsMusicProvider.NetEaseCloudMusic;
        InfLinkPanel.Visibility = isNetEase ? Visibility.Visible : Visibility.Collapsed;
        if (!isNetEase)
            return;

        InfLinkStatusText.Text = NetEaseInfLinkInstaller.GetStatusText();
        var installed = NetEaseInfLinkInstaller.GetInstallState() == NetEaseInfLinkInstallState.Installed;
        InstallInfLinkButton.Content = installed ? "重新安装 InfLink-rs" : "一键安装 InfLink-rs";
    }

    private async void InstallInfLinkButton_Click(object sender, RoutedEventArgs e)
    {
        InstallInfLinkButton.IsEnabled = false;
        var forceReinstall = NetEaseInfLinkInstaller.GetInstallState() == NetEaseInfLinkInstallState.Installed;
        UiFeedback.Show(StatusBar, InfoBarSeverity.Informational, forceReinstall ? "正在重新安装，请稍候…" : "正在安装，请稍候…");

        var installFinished = false;
        try
        {
            var progress = new Progress<string>(msg =>
            {
                if (!installFinished)
                    UiFeedback.Show(StatusBar, InfoBarSeverity.Informational, msg);
            });
            var result = await NetEaseInfLinkInstaller.InstallAsync(progress, forceReinstall);
            installFinished = true;
            UpdateInfLinkUi();
            UiFeedback.Show(
                StatusBar,
                result.Success ? InfoBarSeverity.Success : InfoBarSeverity.Error,
                result.Message);
        }
        catch (Exception ex)
        {
            installFinished = true;
            UiFeedback.Show(StatusBar, InfoBarSeverity.Error, $"安装失败：{ex.Message}");
        }
        finally
        {
            InstallInfLinkButton.IsEnabled = true;
        }
    }

    private async void OpenInfLinkWikiButton_Click(object sender, RoutedEventArgs e) =>
        await Launcher.LaunchUriAsync(new Uri("https://github.com/traceless929/PixelBar/wiki/Lyrics-NetEase#安装-inflink-rs必需"));

    private static IEnumerable<UIElement> BuildGuideSteps(bool isQq)
    {
        var steps = isQq
            ? new[]
            {
                "关闭 EDIFIER TempoHub，USB 连接 PixelBar，在「设置」中选择设备。",
                "用 QQ 音乐播放歌曲（需已缓存歌词，完整播放一遍通常即可）。",
                "下方开启「启用歌词推送」；若先开 PixelBar 后开 QQ 音乐，一般会自动检测，也可点「重新同步」。",
                "若歌词与进度不同步，调整「时间偏移」；长句可开启滚动并选择方向。",
                "仍无歌词时，可开启 QQ 音乐「桌面歌词」作为回退，或检查下方诊断信息。",
            }
            : new[]
            {
                "关闭 EDIFIER TempoHub，USB 连接 PixelBar，在「设置」中选择设备。",
                "点击下方「一键安装 InfLink-rs」（或按 Wiki 手动安装 BetterNCM + InfLink-rs）。",
                "安装完成后重启网易云音乐，确认诊断显示 InfLink-rs（进度 …）。",
                "播放歌曲生成歌词缓存，开启「启用歌词推送」。",
                "若歌词与进度不同步，调整「时间偏移」；长句可开启滚动。",
            };

        var index = 1;
        foreach (var text in steps)
        {
            yield return CreateGuideStep(index, text, index <= 3);
            index++;
        }
    }

    private static StackPanel CreateGuideStep(int index, string text, bool accent)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        panel.Children.Add(new Border
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(14),
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources[
                accent ? "AccentFillColorDefaultBrush" : "AccentFillColorSecondaryBrush"],
            Child = new TextBlock
            {
                Text = index.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            },
        });
        panel.Children.Add(new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        return panel;
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
        ScrollDirectionPanel.Visibility = ScrollSwitch.IsOn && GetSelectedAnimation() == LyricAnimation.None
            ? Visibility.Visible : Visibility.Collapsed;

    private async void OpenWikiButton_Click(object sender, RoutedEventArgs e)
    {
        var url = _selectedProvider == LyricsMusicProvider.NetEaseCloudMusic
            ? "https://github.com/traceless929/PixelBar/wiki/Lyrics-NetEase"
            : "https://github.com/traceless929/PixelBar/wiki/Lyrics-QQMusic";
        await Launcher.LaunchUriAsync(new Uri(url));
    }

    private void ResyncButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnableSwitch.IsOn)
            return;

        LyricsSyncService.Instance.RequestResync();
        var playerName = _selectedProvider == LyricsMusicProvider.NetEaseCloudMusic ? "网易云音乐" : "QQ 音乐";
        if (!PixelBarService.Instance.HasSelectedDevice)
            UiFeedback.Show(StatusBar, InfoBarSeverity.Warning, "尚未选择 PixelBar 设备，请先在设置中连接。");
        else
            UiFeedback.Show(StatusBar, InfoBarSeverity.Informational, $"已触发重新同步，请确保{playerName}正在播放。");
    }

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
        var isNetEase = _selectedProvider == LyricsMusicProvider.NetEaseCloudMusic;
        var playerName = isNetEase ? "网易云音乐" : "QQ 音乐";

        StateText.Text = status switch
        {
            LyricsSyncStatus.Idle => "未启用",
            LyricsSyncStatus.WaitingForPlayer => $"等待 {playerName}…（后台自动检测）",
            LyricsSyncStatus.Paused => $"{playerName} 已暂停",
            LyricsSyncStatus.PlayingWithLyrics => "正在推送歌词",
            LyricsSyncStatus.PlayingFallback => "正在推送（未匹配到歌词，显示歌名）",
            LyricsSyncStatus.Error when !string.IsNullOrWhiteSpace(line) => $"出错：{line}",
            LyricsSyncStatus.Error => "出错",
            _ => status.ToString(),
        };

        SongText.Text = title is null ? "—" : $"{title} · {artist}";
        LineText.Text = string.IsNullOrWhiteSpace(line) ? "—" : line;
        SourceText.Text = source switch
        {
            LyricSource.Desktop => $"来源：{playerName} 桌面歌词",
            LyricSource.QrcCache => "来源：QQ 音乐 qrc 缓存",
            LyricSource.NetEaseApi => "来源：网易云歌词 API（按歌曲 ID）",
            LyricSource.NetEaseCache when diagnostic?.Contains("InfLink-rs", StringComparison.Ordinal) == true
                => "来源：网易云歌词缓存（InfLink-rs 进度）",
            LyricSource.NetEaseCache when diagnostic?.Contains("进度不可用", StringComparison.Ordinal) == true
                => "来源：网易云歌词缓存（SMTC 无进度，桌面锚定）",
            LyricSource.NetEaseCache when diagnostic?.Contains("无 SMTC", StringComparison.Ordinal) == true
                => "来源：网易云歌词缓存（桌面歌词匹配）",
            LyricSource.NetEaseCache => "来源：网易云歌词缓存",
            LyricSource.Fallback => "来源：回退（歌名/歌手）",
            _ => "来源：—",
        };
        DiagnosticText.Text = string.IsNullOrWhiteSpace(diagnostic) ? "—" : diagnostic;
    }
}
