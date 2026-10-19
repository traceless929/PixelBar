using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using PixelBar_App.Helpers;
using PixelBar_App.Services;

namespace PixelBar_App.Pages;

public sealed partial class ClockPage : Page
{
    private bool _loaded;
    private static readonly (int Style, string Title, string Asset)[] Scenes =
    [
        (1, "时间 · 日期星期", "Assets/Scenes/Clock/pixel_clock_0.png"),
        (2, "日期 · 括号日历", "Assets/Scenes/Clock/pixel_clock_1.png"),
        (3, "时间 · 星期箭头", "Assets/Scenes/Clock/pixel_clock_2.png"),
        (4, "还拖? 收你来了", "Assets/Scenes/Clock/pixel_clock_3.png"),
        (5, "早点完成工作", "Assets/Scenes/Clock/pixel_clock_4.png"),
        (6, "啥都没干就已经", "Assets/Scenes/Clock/pixel_clock_5.png"),
        (7, "吉时已到", "Assets/Scenes/Clock/pixel_clock_6.png"),
        (8, "明天的事,明天再说", "Assets/Scenes/Clock/pixel_clock_7.png"),
        (9, "当前时空坐标", "Assets/Scenes/Clock/pixel_clock_8.png"),
        (10, "摸鱼吉时已到", "Assets/Scenes/Clock/pixel_clock_9.png"),
        (11, "看看几点", "Assets/Scenes/Clock/pixel_clock_10.png"),
    ];

    public ClockPage()
    {
        InitializeComponent();
        ClockGrid.ItemsSource = Scenes
            .Select(s => new ClockSceneItem(
                s.Style,
                $"样式 {s.Style}",
                s.Title,
                AssetImages.LoadBitmap(s.Asset)))
            .ToList();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        AutoSyncClockSwitch.IsOn = AppSettingsService.Instance.Current.AutoSyncClockOnStartup;
        _loaded = true;
        ClockTimeSyncService.Instance.StatusChanged += OnSyncStatusChanged;
        RefreshSyncStatus();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        _loaded = false;
        ClockTimeSyncService.Instance.StatusChanged -= OnSyncStatusChanged;
    }

    private void AutoSyncClockSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        if (!_loaded)
            return;
        try
        {
            AppSettingsService.Instance.UpdateAutoSyncClockOnStartup(AutoSyncClockSwitch.IsOn);
            UiFeedback.Show(StatusBar, InfoBarSeverity.Success, AutoSyncClockSwitch.IsOn
                ? "已保存：下次启动应用时自动同步时间。"
                : "已关闭启动校时，仍可点击立即同步时间。");
        }
        catch
        {
            UiFeedback.Show(StatusBar, InfoBarSeverity.Error, "保存失败，请稍后重试。");
        }
    }

    private async void SyncTimeButton_Click(object sender, RoutedEventArgs args)
    {
        try
        {
            await ClockTimeSyncService.Instance.SyncNowAsync();
        }
        catch (Exception ex)
        {
            if (_loaded)
                UiFeedback.Show(StatusBar, InfoBarSeverity.Error,
                    ClockTimeSyncService.Instance.ErrorMessage ?? ex.Message);
        }
    }

    private void OnSyncStatusChanged(object? sender, EventArgs args) =>
        DispatcherQueue.TryEnqueue(() => { if (_loaded) RefreshSyncStatus(); });

    private void RefreshSyncStatus()
    {
        var service = ClockTimeSyncService.Instance;
        SyncTimeButton.IsEnabled = !service.IsSyncing;
        LastSyncText.Text = service.LastSynchronizedAt is { } time
            ? $"最近同步：{time:yyyy-MM-dd HH:mm:ss}"
            : "本次启动尚未同步";
        if (service.IsSyncing)
            UiFeedback.Show(StatusBar, InfoBarSeverity.Informational, "正在同步时间…");
        else if (service.ErrorMessage is { } error)
            UiFeedback.Show(StatusBar, InfoBarSeverity.Error, error);
        else if (service.LastSynchronizedAt is { } sent)
            UiFeedback.Show(StatusBar, InfoBarSeverity.Success, $"已将电脑时间发送给音响：{sent:yyyy-MM-dd HH:mm:ss}。");
    }

    private void ClockGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ClockSceneItem item)
            return;

        try
        {
            PixelBarService.Instance.CreateClient().ShowClock(item.Style);
            UiFeedback.Show(StatusBar, InfoBarSeverity.Success, $"已切换 · {item.Title}");
        }
        catch (Exception ex)
        {
            UiFeedback.Show(StatusBar, InfoBarSeverity.Error, ex.Message);
        }
    }
}

public sealed record ClockSceneItem(int Style, string StyleLabel, string Title, BitmapImage PreviewImage);
