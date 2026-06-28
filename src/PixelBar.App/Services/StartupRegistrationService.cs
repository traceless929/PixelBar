using Microsoft.Win32;

namespace PixelBar_App.Services;

public static class StartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "PixelBar";

    public static bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(ValueName) is string;
    }

    public static void Apply(AppSettings settings)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, true);

        if (!settings.RunAtStartup)
        {
            key.DeleteValue(ValueName, false);
            return;
        }

        key.SetValue(ValueName, BuildLaunchCommand(settings.MinimizeToTrayOnClose));
    }

    public static string BuildLaunchCommand(bool startInTray)
    {
        var exePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("无法确定应用程序路径。");

        return startInTray
            ? $"\"{exePath}\" --tray"
            : $"\"{exePath}\"";
    }
}
