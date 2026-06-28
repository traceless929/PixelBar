namespace PixelBar_App.Helpers;

using Microsoft.UI.Xaml.Controls;

public static class UiFeedback
{
    public static void Show(InfoBar bar, InfoBarSeverity severity, string message)
    {
        bar.Severity = severity;
        bar.Message = message;
        bar.IsOpen = true;
    }

    public static void Hide(InfoBar bar) => bar.IsOpen = false;
}
