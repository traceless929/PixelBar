using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PixelBar_App.Helpers;
using PixelBar_App.Services;

namespace PixelBar_App.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _loadingSettings;

    public SettingsPage()
    {
        InitializeComponent();
        VersionText.Text = $"版本 {GetAppVersion()}";
        Loaded += (_, _) =>
        {
            LoadBehaviorSettings();
            RefreshDevices();
        };
    }

    private void LoadBehaviorSettings()
    {
        _loadingSettings = true;
        var settings = AppSettingsService.Instance.Current;
        RunAtStartupSwitch.IsOn = settings.RunAtStartup;
        MinimizeToTraySwitch.IsOn = settings.MinimizeToTrayOnClose;
        _loadingSettings = false;
    }

    private void RunAtStartupSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings)
            return;

        try
        {
            AppSettingsService.Instance.UpdateRunAtStartup(RunAtStartupSwitch.IsOn);
            UiFeedback.Show(StatusBar, InfoBarSeverity.Success,
                RunAtStartupSwitch.IsOn ? "已开启开机自动启动" : "已关闭开机自动启动");
        }
        catch (Exception ex)
        {
            LoadBehaviorSettings();
            UiFeedback.Show(StatusBar, InfoBarSeverity.Error, ex.Message);
        }
    }

    private void MinimizeToTraySwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings)
            return;

        try
        {
            AppSettingsService.Instance.UpdateMinimizeToTrayOnClose(MinimizeToTraySwitch.IsOn);
            if (!MinimizeToTraySwitch.IsOn)
                TrayIconService.Instance.Hide();
            UiFeedback.Show(StatusBar, InfoBarSeverity.Success,
                MinimizeToTraySwitch.IsOn ? "关闭窗口时将最小化到通知区域" : "关闭窗口时将直接退出应用");
        }
        catch (Exception ex)
        {
            LoadBehaviorSettings();
            UiFeedback.Show(StatusBar, InfoBarSeverity.Error, ex.Message);
        }
    }

    private void ShowWelcomeButton_Click(object sender, RoutedEventArgs e) =>
        App.MainWindowInstance?.NavigateContent(typeof(WelcomePage));

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshDevices();

    private void DeviceList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not DeviceItem item)
            return;

        PixelBarService.Instance.SelectDevice(item.DevicePath, item.DisplayName);
        DeviceList.SelectedItem = item;
        UpdateCurrentDeviceDisplay();
        UiFeedback.Show(StatusBar, InfoBarSeverity.Success, $"已选择 {item.DisplayName}");
    }

    private void RefreshDevices()
    {
        RefreshButton.IsEnabled = false;
        try
        {
            var devices = PixelBarService.Instance.ListDevices();
            var items = devices.Select(d => new DeviceItem(d.DisplayName, d.DevicePath)).ToList();
            DeviceList.ItemsSource = items;

            if (items.Count == 0)
            {
                PixelBarService.Instance.ClearSelection();
                UpdateCurrentDeviceDisplay();
                UiFeedback.Show(StatusBar, InfoBarSeverity.Warning, "未找到 PixelBar。请确认 USB 已连接且驱动正常。");
                return;
            }

            var selectedPath = PixelBarService.Instance.SelectedDevicePath;
            var selected = items.FirstOrDefault(i => i.DevicePath == selectedPath) ?? items[0];
            PixelBarService.Instance.SelectDevice(selected.DevicePath, selected.DisplayName);
            DeviceList.SelectedItem = selected;
            UpdateCurrentDeviceDisplay();
            UiFeedback.Show(StatusBar, InfoBarSeverity.Informational, $"发现 {items.Count} 个设备");
        }
        catch (Exception ex)
        {
            UiFeedback.Show(StatusBar, InfoBarSeverity.Error, ex.Message);
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void UpdateCurrentDeviceDisplay()
    {
        var service = PixelBarService.Instance;
        if (service.HasSelectedDevice)
        {
            CurrentDeviceName.Text = service.SelectedDeviceDisplayName ?? "PixelBar";
            CurrentDevicePath.Text = service.SelectedDevicePath ?? "—";
            return;
        }

        CurrentDeviceName.Text = "未连接";
        CurrentDevicePath.Text = "请在下方列表中选择设备";
    }

    private static string GetAppVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? "1.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}

public sealed record DeviceItem(string DisplayName, string DevicePath);
