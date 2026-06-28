using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PixelBar.Sdk.Protocol;
using PixelBar_App.Helpers;
using PixelBar_App.Services;
using Windows.UI;

namespace PixelBar_App.Pages;

public sealed partial class ScreenColorPage : Page
{
    private static readonly (string Hex, string Label)[] Presets =
    [
        ("#FF0000", "红色"),
        ("#FF8800", "橙色"),
        ("#FFFF00", "黄色"),
        ("#00FF00", "绿色"),
        ("#00FFFF", "青色"),
        ("#0077EE", "天蓝"),
        ("#0000FF", "蓝色"),
        ("#FF00FF", "品红"),
        ("#FFFFFF", "白色"),
    ];

    public ScreenColorPage()
    {
        InitializeComponent();
        PresetRepeater.ItemsSource = Presets
            .Select(p => new ColorPresetItem(p.Hex, p.Label, new SolidColorBrush(ParseColor(p.Hex))))
            .ToList();
        ColorPicker.Color = ParseColor("#0077EE");
    }

    private void PresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string hex })
            return;

        ColorPicker.Color = ParseColor(hex);
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyButton.IsEnabled = false;
        try
        {
            var c = ColorPicker.Color;
            var color = new RgbColor(c.R, c.G, c.B);
            var syncLight = SyncLightCheckBox.IsChecked == true;

            PixelBarService.Instance.CreateClient().SetScreenColor(color, syncLight);

            var detail = syncLight ? " · 已尝试同步氛围灯" : string.Empty;
            UiFeedback.Show(StatusBar, InfoBarSeverity.Success, $"屏色 #{color.R:X2}{color.G:X2}{color.B:X2}{detail}");
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

    private static Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromArgb(
            255,
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));
    }
}

public sealed record ColorPresetItem(string Hex, string Label, SolidColorBrush Brush);
