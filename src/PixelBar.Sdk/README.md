# PixelBar.Sdk

花再 **Halo PixelBar** 的 Windows HID 控制 SDK，封装文字、灯光、时钟与频谱等已验证协议。

## 开放使用

**PixelBar.Sdk 对社区开放**，供任意 .NET 项目引用。协议抓包、报文编码与 checksum 均已内置，你可以**跳过底层 HID 流程**，直接调用 `PixelBarClient` 做二次开发——例如自定义歌词屏、RGB 神光同步桥接、Stream Deck 插件、Home Assistant 网关等。

本仓库的 CLI 与 WinUI 客户端本身也是 SDK 的消费者。若你做了有趣的上位机或集成，欢迎提 Issue 分享链接，或通过 PR 将通用能力贡献回 SDK，促进 PixelBar 生态多元化。

## 安装

```bash
# 本地引用（开发）
dotnet add package PixelBar.Sdk --source ./nupkg

# 或项目引用
dotnet add reference path/to/PixelBar.Sdk/PixelBar.Sdk.csproj
```

打包 SDK：

```bash
dotnet pack src/PixelBar.Sdk/PixelBar.Sdk.csproj -c Release -o ./nupkg
```

## 快速开始

```csharp
using PixelBar.Sdk;
using PixelBar.Sdk.Protocol;

// 发现设备并连接
var devices = PixelBarSdk.DiscoverDevices();
using var client = PixelBarSdk.Connect(devices[0].DevicePath);

// 文字（静态居中）
client.ShowText("你好 PixelBar");

// 文字（左对齐 / 滚动）
client.ShowText("Hello PixelBar", alignment: PixelTextAlignment.Left);
client.ShowText("滚动字幕", effect: TextDisplayEffect.Scroll,
    scrollDirection: TextScrollDirection.RightToLeft);

// 灯光（mode 1–2 可调速度）
client.SetLight(LightMode.PureStatic, RgbColor.FromHex("#00FF00"));

// 时钟 / 频谱 / 屏色
client.ShowClock(3);
client.ShowSpectrum(1);
client.SetScreenColor(RgbColor.FromHex("#0077EE"));
```

## 协议层（高级）

低层编解码器位于 `PixelBar.Sdk.Protocol`，可用于自定义流水线或 `dry-run` 调试：

```csharp
var packets = new[]
{
    TextLayoutReportEncoder.EncodeStatic(PixelTextAlignment.Center),
    TextReportEncoder.Encode("Hello"),
};
foreach (var p in packets)
    client.SendReport(p);
```

## 要求

- Windows 10+
- .NET 10（`net10.0-windows`）
- 设备 VID/PID：`0x2D99` / `0xA106`

## 设备能力

| API | 说明 |
|-----|------|
| `ShowText` | 文字 + 布局（静态/滚动、对齐） |
| `SetLight` | 6 种灯效 + 颜色（速度仅 mode 1–2） |
| `ShowClock` / `ShowPattern` | 时钟样式 1–11 |
| `ShowSpectrum` | 频谱样式 1–4 |
| `SetScreenColor` | 像素屏主题色（`0xEF 00 04 03` + RGB） |

上位机模板类场景（游戏、宠物等）及 `0x17` 帧上传不在 SDK 支持范围内。

使用 SDK 即表示你理解并接受 [项目免责声明](../../README.md#免责声明)（第三方逆向/互操作项目，与 EDIFIER 官方无关，按现状提供、风险自负）。

### 像素屏颜色

```csharp
client.SetScreenColor(RgbColor.FromHex("#0077EE"));
client.SetScreenColor(RgbColor.FromHex("#0077EE"), syncAtmosphereLight: true);
```
