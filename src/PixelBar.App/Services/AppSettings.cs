namespace PixelBar_App.Services;

public sealed class AppSettings
{
    public bool RunAtStartup { get; set; }

    public bool MinimizeToTrayOnClose { get; set; }

    public bool HasCompletedWelcome { get; set; }
}
