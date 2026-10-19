namespace PixelBar_App.Services;

/// <summary>启动校时每次进程成功执行一次；手动校时可随时执行。</summary>
public sealed class ClockTimeSyncService
{
    public static ClockTimeSyncService Instance { get; } = new(
        () => AppSettingsService.Instance.Current.AutoSyncClockOnStartup,
        () => PixelBarService.Instance.SelectedDevicePath,
        path => new PixelBar.Sdk.PixelBarClient(path).SyncTime());

    private readonly Func<bool> _autoEnabled;
    private readonly Func<string?> _devicePath;
    private readonly Func<string, DateTimeOffset> _send;
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    private bool _initialized;
    private bool _startupSynchronized;

    internal ClockTimeSyncService(Func<bool> autoEnabled, Func<string?> devicePath, Func<string, DateTimeOffset> send)
    {
        _autoEnabled = autoEnabled;
        _devicePath = devicePath;
        _send = send;
    }

    public event EventHandler? StatusChanged;
    public bool IsSyncing { get; private set; }
    public DateTimeOffset? LastSynchronizedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;
        PixelBarService.Instance.DeviceSelectionChanged += OnDeviceSelectionChanged;
        if (_autoEnabled())
        {
            try
            {
                // 托盘启动也要先完成设备选择，不能依赖页面 Loaded。
                PixelBarService.Instance.EnsureDefaultDevice();
            }
            catch
            {
                ErrorMessage = "暂时无法连接音响，请在设置中重新选择设备，或点击立即同步重试。";
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        _ = TryStartupSyncAsync();
    }

    private void OnDeviceSelectionChanged(object? sender, EventArgs args) => _ = TryStartupSyncAsync();

    public async Task TryStartupSyncAsync()
    {
        await _syncGate.WaitAsync();
        try
        {
            var path = _devicePath();
            if (!_autoEnabled() || _startupSynchronized || string.IsNullOrEmpty(path))
                return;
            await SendAsync(path);
        }
        catch
        {
            // 启动校时失败不能阻止主窗口、托盘或歌词服务启动。
        }
        finally
        {
            _syncGate.Release();
        }
    }

    public async Task SyncNowAsync()
    {
        await _syncGate.WaitAsync();
        try
        {
            var path = _devicePath();
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("请先在设置中连接 PixelBar。");
            await SendAsync(path);
        }
        finally
        {
            _syncGate.Release();
        }
    }

    private async Task SendAsync(string path)
    {
        IsSyncing = true;
        ErrorMessage = null;
        StatusChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            LastSynchronizedAt = await Task.Run(() => _send(path));
            _startupSynchronized = true;
        }
        catch
        {
            ErrorMessage = "同步失败，请检查 USB 连接，或退出 TempoHub 后重试。";
            throw;
        }
        finally
        {
            IsSyncing = false;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
