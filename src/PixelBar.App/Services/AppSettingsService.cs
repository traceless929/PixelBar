using System.Text.Json;

namespace PixelBar_App.Services;

public sealed class AppSettingsService
{
    public static AppSettingsService Instance { get; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _settingsPath;

    public AppSettings Current { get; private set; } = new();

    public event EventHandler? SettingsChanged;

    private AppSettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PixelBar");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
        Load();
    }

    public void Load()
    {
        if (!File.Exists(_settingsPath))
        {
            Current = new AppSettings();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            Current = new AppSettings();
        }

        if (Current.RunAtStartup)
            StartupRegistrationService.Apply(Current);
    }

    public void Save(AppSettings settings)
    {
        Current = settings;
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateRunAtStartup(bool enabled)
    {
        Current.RunAtStartup = enabled;
        Save(Current);
        StartupRegistrationService.Apply(Current);
    }

    public void UpdateMinimizeToTrayOnClose(bool enabled)
    {
        Current.MinimizeToTrayOnClose = enabled;
        Save(Current);
        if (Current.RunAtStartup)
            StartupRegistrationService.Apply(Current);
    }

    public void CompleteWelcome()
    {
        Current.HasCompletedWelcome = true;
        Save(Current);
    }

    public void ResetWelcome()
    {
        Current.HasCompletedWelcome = false;
        Save(Current);
    }
}
