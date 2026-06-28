using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using PixelBar_App.Helpers;
using PixelBar_App.Services;

namespace PixelBar_App.Pages;

public sealed partial class ClockPage : Page
{
    private static readonly (int Style, string Title, string Asset)[] Scenes =
    [
        (1, "时间 · 日期星期", "Assets/Scenes/Clock/pixel_clock_0.png"),
        (2, "日期 · 括号日历", "Assets/Scenes/Clock/pixel_clock_1.png"),
        (3, "时间 · 星期箭头", "Assets/Scenes/Clock/pixel_clock_2.png"),
        (4, "还拖? 收你来了", "Assets/Scenes/Clock/pixel_clock_3.png"),
        (5, "早点完成工作", "Assets/Scenes/Clock/pixel_clock_4.png"),
        (6, "啥都没干就已经", "Assets/Scenes/Clock/pixel_clock_5.png"),
        (7, "吉时已到", "Assets/Scenes/Clock/pixel_clock_6.png"),
        (8, "明天的事,明天再说", "Assets/Scenes/Clock/pixel_clock_7.png"),
        (9, "当前时空坐标", "Assets/Scenes/Clock/pixel_clock_8.png"),
        (10, "摸鱼吉时已到", "Assets/Scenes/Clock/pixel_clock_9.png"),
        (11, "看看几点", "Assets/Scenes/Clock/pixel_clock_10.png"),
    ];

    public ClockPage()
    {
        InitializeComponent();
        ClockGrid.ItemsSource = Scenes
            .Select(s => new ClockSceneItem(
                s.Style,
                $"样式 {s.Style}",
                s.Title,
                new BitmapImage(new Uri($"ms-appx:///{s.Asset}"))))
            .ToList();
    }

    private void ClockGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ClockSceneItem item)
            return;

        try
        {
            PixelBarService.Instance.CreateClient().ShowClock(item.Style);
            UiFeedback.Show(StatusBar, InfoBarSeverity.Success, $"已切换 · {item.Title}");
        }
        catch (Exception ex)
        {
            UiFeedback.Show(StatusBar, InfoBarSeverity.Error, ex.Message);
        }
    }
}

public sealed record ClockSceneItem(int Style, string StyleLabel, string Title, BitmapImage PreviewImage);
