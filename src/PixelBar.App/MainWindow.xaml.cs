using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PixelBar_App.Pages;
using PixelBar_App.Services;
using Windows.UI;

namespace PixelBar_App;

public sealed partial class MainWindow : Window
{
    private const int DefaultWidthCap = 1520;
    private const int DefaultHeightCap = 980;
    private const int MinimumWidth = 1120;
    private const int MinimumHeight = 780;
    private const double DefaultWidthRatio = 0.90;
    private const double DefaultHeightRatio = 0.90;

    private bool _windowSized;
    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Closing += OnAppWindowClosing;

        ConfigureWindowSize();

        PixelBarService.Instance.DeviceSelectionChanged += (_, _) => RefreshConnectionStatus();
        Activated += OnFirstActivated;
    }

    public void HideToTray(bool showNotification = true)
    {
        AppWindow.Hide();
        TrayIconService.Instance.Show(
            showNotification ? "PixelBar 已最小化到通知区域，双击图标可重新打开。" : null);
    }

    public void ShowFromTray()
    {
        AppWindow.Show();
        Activate();
        TrayIconService.Instance.Hide();
    }

    public void RequestExit()
    {
        _forceClose = true;
        TrayIconService.Instance.Dispose();
        Application.Current.Exit();
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_forceClose)
            return;

        if (AppSettingsService.Instance.Current.MinimizeToTrayOnClose)
        {
            args.Cancel = true;
            HideToTray();
        }
    }

    private void OnFirstActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_windowSized)
            return;

        _windowSized = true;
        ConfigureWindowSize();
        Activated -= OnFirstActivated;
    }

    private void ConfigureWindowSize()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = MinimumWidth;
            presenter.PreferredMinimumHeight = MinimumHeight;
        }

        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea is null)
        {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(DefaultWidthCap, DefaultHeightCap));
            return;
        }

        var workArea = displayArea.WorkArea;
        var maxWidth = workArea.Width - 48;
        var maxHeight = workArea.Height - 48;

        var width = (int)Math.Min(DefaultWidthCap, Math.Max(MinimumWidth, workArea.Width * DefaultWidthRatio));
        var height = (int)Math.Min(DefaultHeightCap, Math.Max(MinimumHeight, workArea.Height * DefaultHeightRatio));
        width = Math.Min(width, maxWidth);
        height = Math.Min(height, maxHeight);

        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            workArea.X + (workArea.Width - width) / 2,
            workArea.Y + (workArea.Height - height) / 2,
            width,
            height));
    }

    public void NavigateContent(Type pageType) => NavFrame.Navigate(pageType);

    public void NavigateToTag(string tag)
    {
        if (FindNavItem(NavView.MenuItems, tag) is NavigationViewItem menuItem)
        {
            NavView.SelectedItem = menuItem;
            return;
        }

        if (FindNavItem(NavView.FooterMenuItems, tag) is NavigationViewItem footerItem)
            NavView.SelectedItem = footerItem;
    }

    private static NavigationViewItem? FindNavItem(object items, string tag)
    {
        if (items is NavigationViewItem direct && tag.Equals(direct.Tag as string, StringComparison.Ordinal))
            return direct;

        if (items is IEnumerable<object> collection)
        {
            foreach (var entry in collection)
            {
                if (entry is not NavigationViewItem item)
                    continue;

                if (tag.Equals(item.Tag as string, StringComparison.Ordinal))
                    return item;

                if (item.MenuItems.Count > 0 && FindNavItem(item.MenuItems, tag) is NavigationViewItem nested)
                    return nested;
            }
        }

        return null;
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        PixelBarService.Instance.EnsureDefaultDevice();
        RefreshConnectionStatus();

        if (AppSettingsService.Instance.Current.HasCompletedWelcome)
            NavFrame.Navigate(typeof(HomePage));
        else
            NavFrame.Navigate(typeof(WelcomePage));
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        NavFrame.GoBack();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
            return;

        var tag = item.Tag as string;
        Type? pageType = tag switch
        {
            "text" => typeof(HomePage),
            "light" => typeof(LightPage),
            "lyrics" => typeof(LyricsPage),
            "clock" => typeof(ClockPage),
            "spectrum" => typeof(PatternPage),
            "screen-color" => typeof(ScreenColorPage),
            "settings" => typeof(SettingsPage),
            _ => null,
        };

        if (pageType is not null)
            NavFrame.Navigate(pageType);
    }

    private void RefreshConnectionStatus()
    {
        var service = PixelBarService.Instance;
        if (service.HasSelectedDevice)
        {
            ConnectionDot.Fill = new SolidColorBrush(Color.FromArgb(255, 16, 124, 65));
            ConnectionText.Text = $"已连接 · {service.SelectedDeviceDisplayName}";
            return;
        }

        ConnectionDot.Fill = new SolidColorBrush(Color.FromArgb(255, 196, 43, 28));
        ConnectionText.Text = "未连接设备 · 请前往设置";
    }
}
