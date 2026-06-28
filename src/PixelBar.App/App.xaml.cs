using Microsoft.UI.Xaml;
using PixelBar_App.Services;

namespace PixelBar_App;

public partial class App : Application
{
    public static MainWindow? MainWindowInstance { get; private set; }

    public App()
    {
        InitializeComponent();
        AppSettingsService.Instance.Load();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var startInTray = ShouldStartInTray(args.Arguments);

        MainWindowInstance = new MainWindow();
        TrayIconService.Instance.Initialize(
            () => MainWindowInstance?.ShowFromTray(),
            () => MainWindowInstance?.RequestExit());

        if (startInTray)
            MainWindowInstance.HideToTray(showNotification: false);
        else
            MainWindowInstance.Activate();
    }

    private static bool ShouldStartInTray(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return false;

        return arguments.Contains("--tray", StringComparison.OrdinalIgnoreCase);
    }
}
