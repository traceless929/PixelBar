using System.Runtime.Versioning;

using PixelBar.Sdk.Devices;

using PixelBar.Sdk.Protocol;

namespace PixelBar.Sdk;

/// <summary>
/// 花再 Halo PixelBar 设备控制客户端。
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class PixelBarClient(string? devicePath = null)
{
    /// <summary>当前绑定的 HID 设备路径；为 null 时发送会使用第一个可用端点。</summary>
    public string? DevicePath { get; } = devicePath;

    /// <summary>在像素屏显示文字（可选静态/滚动与对齐方式）。</summary>
    public void ShowText(
        string text,
        TextDisplayEffect effect = TextDisplayEffect.Static,
        PixelTextAlignment alignment = PixelTextAlignment.Center,
        TextScrollDirection scrollDirection = TextScrollDirection.LeftToRight)
    {
        var layout = effect == TextDisplayEffect.Static
            ? TextLayoutReportEncoder.EncodeStatic(alignment)
            : TextLayoutReportEncoder.EncodeScroll(scrollDirection);

        HidReportTransport.Send(layout, DevicePath);
        HidReportTransport.Send(TextReportEncoder.Encode(text), DevicePath);
    }

    /// <summary>用固件内置的进入/退出动画显示歌词，支持 A106 与实测 2026 款 A160 的 EC 协议。</summary>
    public void ShowLyric(string text, LyricAnimation animation = LyricAnimation.BottomToTop)
    {
        var path = DevicePath ?? PixelBarDiscovery.FindPrimary()?.DevicePath
            ?? throw new InvalidOperationException("No PixelBar screen endpoint found.");
        foreach (var report in LyricReportEncoder.Encode(text, animation))
            HidReportTransport.Send(report, path);
    }

    /// <summary>关闭固件歌词叠层，恢复普通文字/场景显示。</summary>
    public void DisableLyricAnimation() =>
        HidReportTransport.Send(LyricReportEncoder.EncodeEnable(false), DevicePath);

    /// <summary>设置 RGB 灯光（官方 0x77 + 设备 0x6B）。</summary>
    public void SetLight(LightMode mode, RgbColor color, byte speed = RgbReportEncoder.DefaultSpeed)
    {
        HidReportTransport.Send(TempoHubLightReportEncoder.Encode(color, speed), DevicePath);
        HidReportTransport.Send(RgbReportEncoder.Encode(mode, color, speed), DevicePath);
    }

    /// <summary>切换时钟图案 1–11（旧版 F0 B4 C8 协议）。</summary>
    public void ShowPattern(int pattern) =>
        HidReportTransport.Send(PatternReportEncoder.Encode(pattern), DevicePath);

    /// <summary>切换频谱样式 1–4。</summary>
    public void ShowSpectrum(int style) =>
        HidReportTransport.Send(SceneReportEncoder.EncodeSpectrum(style), DevicePath);

    /// <summary>切换时钟样式 1–11（固件内置 F0 B4 C8）。</summary>
    public void ShowClock(int style) =>
        HidReportTransport.Send(PatternReportEncoder.Encode(style), DevicePath);

    /// <summary>同步音响时间，默认使用电脑当前本地时间，返回实际发送的时间。</summary>
    public DateTimeOffset SyncTime(DateTimeOffset? localTime = null)
    {
        var time = localTime ?? DateTimeOffset.Now;
        HidReportTransport.Send(ClockSyncReportEncoder.Encode(time), DevicePath);
        return time;
    }

    /// <summary>?????????0xEF 00 04 03 + RGB???? screen_color_log.txt ???</summary>
    public void SetScreenColor(RgbColor color, bool syncAtmosphereLight = false)
    {
        var sequence = syncAtmosphereLight
            ? ScreenColorReportEncoder.EncodeSetColorWithAtmosphereSync(color)
            : ScreenColorReportEncoder.EncodeSetColor(color);

        foreach (var packet in sequence)
            HidReportTransport.Send(packet, DevicePath);
    }

    /// <summary>发送原始 64 字节 HID 报告（高级用法）。</summary>
    public void SendReport(ReadOnlySpan<byte> report) =>
        HidReportTransport.Send(report, DevicePath);

    /// <summary>枚举已连接的屏幕端点。</summary>
    public static IReadOnlyList<HidEndpoint> ListDevices() =>
        PixelBarDiscovery.ListScreenEndpoints();
}
