using System.Runtime.InteropServices;

namespace PixelBar_App.Services;

public sealed class TrayIconService : IDisposable
{
    public static TrayIconService Instance { get; } = new();

    private const int WmUser = 0x0400;
    private const int WmTrayIcon = WmUser + 1;
    private const int WmCommand = 0x0111;
    private const int WmLButtonDblClk = 0x0203;
    private const int WmRButtonUp = 0x0205;

    private const int NimAdd = 0x00000000;
    private const int NimModify = 0x00000001;
    private const int NimDelete = 0x00000002;

    private const int NifMessage = 0x00000001;
    private const int NifIcon = 0x00000002;
    private const int NifTip = 0x00000004;
    private const int NifInfo = 0x00000010;

    private const int MiString = 0x00000000;
    private const int TpmRightButton = 0x0002;
    private const int TpmReturnCmd = 0x0100;

    private const int IdShow = 1001;
    private const int IdExit = 1002;

    private const uint ImageIcon = 1;
    private const uint LrLoadFromFile = 0x00000010;
    private const uint LrDefaultSize = 0x00000040;

    private static readonly IntPtr HwndMessage = new(-3);

    private readonly WindowProcedureDelegate _wndProcDelegate;
    private Action? _showMainWindow;
    private Action? _exitApplication;
    private IntPtr _messageWindow;
    private IntPtr _iconHandle;
    private bool _added;

    private TrayIconService()
    {
        _wndProcDelegate = HandleWindowMessage;
        _messageWindow = CreateMessageWindow();
    }

    public void Initialize(Action showMainWindow, Action exitApplication)
    {
        _showMainWindow = showMainWindow;
        _exitApplication = exitApplication;
    }

    public void Show(string? balloonMessage = null)
    {
        EnsureIconLoaded();
        var data = CreateNotifyData();
        data.uFlags = NifMessage | NifIcon | NifTip;
        data.hIcon = _iconHandle;
        data.szTip = string.IsNullOrEmpty(balloonMessage) ? "PixelBar" : balloonMessage;

        if (!_added)
        {
            ShellNotifyIcon(NimAdd, ref data);
            _added = true;
            return;
        }

        ShellNotifyIcon(NimModify, ref data);
    }

    public void Hide()
    {
        if (!_added)
            return;

        var data = CreateNotifyData();
        ShellNotifyIcon(NimDelete, ref data);
        _added = false;
    }

    public void Dispose()
    {
        Hide();
        if (_iconHandle != IntPtr.Zero)
        {
            DestroyIcon(_iconHandle);
            _iconHandle = IntPtr.Zero;
        }

        if (_messageWindow != IntPtr.Zero)
        {
            DestroyWindow(_messageWindow);
            _messageWindow = IntPtr.Zero;
        }
    }

    private void EnsureIconLoaded()
    {
        if (_iconHandle != IntPtr.Zero)
            return;

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        _iconHandle = File.Exists(iconPath)
            ? LoadImage(IntPtr.Zero, iconPath, ImageIcon, 0, 0, LrLoadFromFile | LrDefaultSize)
            : LoadIcon(IntPtr.Zero, new IntPtr(32512)); // IDI_APPLICATION
    }

    private NotifyIconData CreateNotifyData() =>
        new()
        {
            cbSize = (uint)Marshal.SizeOf<NotifyIconData>(),
            hWnd = _messageWindow,
            uID = 1,
            uCallbackMessage = WmTrayIcon,
        };

    private IntPtr CreateMessageWindow()
    {
        const int csHRedraw = 0x0002;
        const int csVRedraw = 0x0001;

        var className = "PixelBarTrayHost_" + Guid.NewGuid().ToString("N");
        var wc = new WndClass
        {
            lpfnWndProc = _wndProcDelegate,
            lpszClassName = className,
            style = csHRedraw | csVRedraw,
        };

        RegisterClassW(ref wc);
        return CreateWindowExW(
            0,
            className,
            string.Empty,
            0,
            0,
            0,
            0,
            0,
            HwndMessage,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);
    }

    private IntPtr HandleWindowMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == WmTrayIcon)
        {
            var mouseMessage = (uint)lParam.ToInt64();
            if (mouseMessage == WmLButtonDblClk)
                _showMainWindow?.Invoke();
            else if (mouseMessage == WmRButtonUp)
                ShowContextMenu();
            return IntPtr.Zero;
        }

        if (message == WmCommand)
        {
            switch (wParam.ToInt32() & 0xFFFF)
            {
                case IdShow:
                    _showMainWindow?.Invoke();
                    break;
                case IdExit:
                    _exitApplication?.Invoke();
                    break;
            }

            return IntPtr.Zero;
        }

        return DefWindowProcW(hWnd, message, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        var menu = CreatePopupMenu();
        AppendMenuW(menu, MiString, IdShow, "显示主窗口");
        AppendMenuW(menu, MiString, IdExit, "退出 PixelBar");

        GetCursorPos(out var point);
        SetForegroundWindow(_messageWindow);
        var command = TrackPopupMenuEx(
            menu,
            TpmRightButton | TpmReturnCmd,
            point.X,
            point.Y,
            _messageWindow,
            IntPtr.Zero);
        DestroyMenu(menu);

        if (command != 0)
            SendMessageW(_messageWindow, WmCommand, new IntPtr(command), IntPtr.Zero);
    }

    private delegate IntPtr WindowProcedureDelegate(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClass
    {
        public uint style;
        public WindowProcedureDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;

        public uint uVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassW(ref WndClass lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(
        int dwExStyle,
        string lpClassName,
        string lpWindowName,
        int dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImage(
        IntPtr hInst,
        string name,
        uint type,
        int cx,
        int cy,
        uint fuLoad);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NotifyIconData lpData);

    private static bool ShellNotifyIcon(int message, ref NotifyIconData data) =>
        Shell_NotifyIcon(message, ref data);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, int uIdNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(
        IntPtr hmenu,
        uint fuFlags,
        int x,
        int y,
        IntPtr hwnd,
        IntPtr lptpm);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
