using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PixelBar_App.Helpers;
using PixelBar_App.Services;

namespace PixelBar_App.Pages;

public sealed partial class WelcomePage : Page
{
    public WelcomePage()
    {
        InitializeComponent();
        WelcomeLogoImage.Source = AssetImages.LoadBitmap("Assets/AppLogo.png");
    }

    private void ConnectDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        FinishWelcomeIfRequested();
        App.MainWindowInstance?.NavigateToTag("settings");
    }

    private void EnterAppButton_Click(object sender, RoutedEventArgs e)
    {
        FinishWelcomeIfRequested();
        App.MainWindowInstance?.NavigateToTag("text");
    }

    private void FinishWelcomeIfRequested()
    {
        if (SkipWelcomeCheckBox.IsChecked == true)
            AppSettingsService.Instance.CompleteWelcome();
    }
}
