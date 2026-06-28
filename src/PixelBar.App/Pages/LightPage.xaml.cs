using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PixelBar.Sdk.Protocol;
using PixelBar_App.Helpers;
using PixelBar_App.Services;

namespace PixelBar_App.Pages;

public sealed partial class LightPage : Page
{
    private static readonly (LightMode Mode, string Label)[] Modes =
    [
        (LightMode.AmbientBreathing, "氛围呼吸"),
        (LightMode.ColorfulTide, "幻彩潮汐"),
        (LightMode.PureStatic, "纯色静光"),
        (LightMode.ColorfulRipple, "炫彩涟漪"),
        (LightMode.FlowingLight, "流光逐影"),
        (LightMode.DynamicShadow, "动态光影"),
    ];

    public LightPage()
    {
        InitializeComponent();
        foreach (var (_, label) in Modes)
            ModeBox.Items.Add(label);

        ColorPicker.Color = Windows.UI.Color.FromArgb(255, 0, 255, 0);
        UpdateSpeedPanel();
    }

    private void ModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateSpeedPanel();

    private void UpdateSpeedPanel()
    {
        if (ModeBox.SelectedIndex < 0)
            return;

        var enabled = RgbReportEncoder.SupportsSpeed(Modes[ModeBox.SelectedIndex].Mode);
        SpeedCard.Opacity = enabled ? 1 : 0.45;
        SpeedSlider.IsEnabled = enabled;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyButton.IsEnabled = false;
        try
        {
            var mode = Modes[ModeBox.SelectedIndex].Mode;
            var c = ColorPicker.Color;
            var color = new RgbColor(c.R, c.G, c.B);
            var speed = (byte)SpeedSlider.Value;

            PixelBarService.Instance.CreateClient().SetLight(mode, color, speed);

            var detail = RgbReportEncoder.SupportsSpeed(mode)
                ? $"#{color.R:X2}{color.G:X2}{color.B:X2} · 速度 {speed}"
                : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            UiFeedback.Show(StatusBar, InfoBarSeverity.Success, $"{Modes[ModeBox.SelectedIndex].Label} · {detail}");
        }
        catch (Exception ex)
        {
            UiFeedback.Show(StatusBar, InfoBarSeverity.Error, ex.Message);
        }
        finally
        {
            ApplyButton.IsEnabled = true;
        }
    }
}
