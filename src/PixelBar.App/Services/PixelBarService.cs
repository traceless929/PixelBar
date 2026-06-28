using PixelBar.Sdk;
using PixelBar.Sdk.Devices;

namespace PixelBar_App.Services;

public sealed class PixelBarService
{
    public static PixelBarService Instance { get; } = new();

    public event EventHandler? DeviceSelectionChanged;

    public string? SelectedDevicePath { get; private set; }

    public string? SelectedDeviceDisplayName { get; private set; }

    public bool HasSelectedDevice => !string.IsNullOrEmpty(SelectedDevicePath);

    public PixelBarClient CreateClient()
    {
        if (string.IsNullOrEmpty(SelectedDevicePath))
            throw new InvalidOperationException("未选择设备。请在「设置」中连接 PixelBar。");

        return new(SelectedDevicePath);
    }

    public IReadOnlyList<HidEndpoint> ListDevices() => PixelBarClient.ListDevices();

    public void SelectDevice(string devicePath, string displayName)
    {
        SelectedDevicePath = devicePath;
        SelectedDeviceDisplayName = displayName;
        DeviceSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearSelection()
    {
        SelectedDevicePath = null;
        SelectedDeviceDisplayName = null;
        DeviceSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void EnsureDefaultDevice()
    {
        if (HasSelectedDevice)
            return;

        var devices = ListDevices();
        if (devices.Count > 0)
            SelectDevice(devices[0].DevicePath, devices[0].DisplayName);
    }
}
