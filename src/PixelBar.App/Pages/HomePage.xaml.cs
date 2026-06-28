using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PixelBar.Sdk.Protocol;
using PixelBar_App.Helpers;
using PixelBar_App.Services;

namespace PixelBar_App.Pages;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        UpdateEffectPanels();
        UpdateCharCount();
    }

    private void TextInput_TextChanged(object sender, TextChangedEventArgs e) => UpdateCharCount();

    private void UpdateCharCount()
    {
        var len = TextInput.Text?.Length ?? 0;
        CharCountText.Text = $"{len} / 16";
    }

    private void EffectGroup_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateEffectPanels();

    private void UpdateEffectPanels()
    {
        var isStatic = GetSelectedEffect() == TextDisplayEffect.Static;
        AlignPanel.Visibility = isStatic ? Visibility.Visible : Visibility.Collapsed;
        ScrollPanel.Visibility = isStatic ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var text = TextInput.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            UiFeedback.Show(StatusBar, InfoBarSeverity.Warning, "请输入要显示的文字。");
            return;
        }

        SendButton.IsEnabled = false;
        try
        {
            var effect = GetSelectedEffect();
            var client = PixelBarService.Instance.CreateClient();
            if (effect == TextDisplayEffect.Static)
                client.ShowText(text, effect, GetSelectedAlignment());
            else
                client.ShowText(text, effect, scrollDirection: GetSelectedScrollDirection());

            UiFeedback.Show(StatusBar, InfoBarSeverity.Success, $"已发送到屏幕：{text}");
        }
        catch (Exception ex)
        {
            UiFeedback.Show(StatusBar, InfoBarSeverity.Error, ex.Message);
        }
        finally
        {
            SendButton.IsEnabled = true;
        }
    }

    private TextDisplayEffect GetSelectedEffect()
    {
        var tag = (EffectGroup.SelectedItem as RadioButton)?.Tag as string;
        return tag == "scroll" ? TextDisplayEffect.Scroll : TextDisplayEffect.Static;
    }

    private static PixelTextAlignment GetSelectedAlignment(RadioButtons group) =>
        ((group.SelectedItem as RadioButton)?.Tag as string) switch
        {
            "left" => PixelTextAlignment.Left,
            "right" => PixelTextAlignment.Right,
            "justify" => PixelTextAlignment.Justify,
            _ => PixelTextAlignment.Center,
        };

    private PixelTextAlignment GetSelectedAlignment() => GetSelectedAlignment(AlignGroup);

    private TextScrollDirection GetSelectedScrollDirection()
    {
        var tag = (ScrollDirectionGroup.SelectedItem as RadioButton)?.Tag as string;
        return tag == "rtl" ? TextScrollDirection.RightToLeft : TextScrollDirection.LeftToRight;
    }
}
