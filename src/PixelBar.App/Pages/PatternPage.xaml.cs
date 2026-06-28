using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using PixelBar_App.Helpers;
using PixelBar_App.Services;

namespace PixelBar_App.Pages;

public sealed partial class PatternPage : Page
{
    private static readonly (int Style, string Title, string Asset)[] Scenes =
    [
        (1, "柱状频谱", "Assets/Scenes/pixel_spectrum_0.png"),
        (2, "点阵波浪", "Assets/Scenes/pixel_spectrum_1.png"),
        (3, "中心对称波形", "Assets/Scenes/pixel_spectrum_2.png"),
        (4, "密集波形", "Assets/Scenes/pixel_spectrum_3.png"),
    ];

    public PatternPage()
    {
        InitializeComponent();
        SpectrumGrid.ItemsSource = Scenes
            .Select(s => new SpectrumSceneItem(
                s.Style,
                $"样式 {s.Style}",
                s.Title,
                new BitmapImage(new Uri($"ms-appx:///{s.Asset}"))))
            .ToList();
    }

    private void SpectrumGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not SpectrumSceneItem item)
            return;

        try
        {
            PixelBarService.Instance.CreateClient().ShowSpectrum(item.Style);
            UiFeedback.Show(StatusBar, InfoBarSeverity.Success, $"已切换 · {item.Title}");
        }
        catch (Exception ex)
        {
            UiFeedback.Show(StatusBar, InfoBarSeverity.Error, ex.Message);
        }
    }
}

public sealed record SpectrumSceneItem(int Style, string StyleLabel, string Title, BitmapImage PreviewImage);
