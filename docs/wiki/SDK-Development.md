# SDK 开发

**PixelBar.Sdk** 对社区开放。协议抓包、报文编码与 checksum 已内置，可直接二次开发。

## 从 GitHub Packages 安装

1. 创建 GitHub Personal Access Token（勾选 `read:packages`）
2. 添加 NuGet 源：

```powershell
dotnet nuget add source "https://nuget.pkg.github.com/traceless929/index.json" `
  --name github-pixelbar `
  --username traceless929 `
  --password YOUR_GITHUB_TOKEN `
  --store-password-in-clear-text
```

3. 安装包：

```bash
dotnet add package PixelBar.Sdk --version 0.0.2
```

也可参考仓库 [`nuget.config.example`](https://github.com/traceless929/PixelBar/blob/main/nuget.config.example)。

## 项目引用（开发）

```bash
git clone https://github.com/traceless929/PixelBar.git
dotnet add YourApp.csproj reference path/to/PixelBar.Sdk/PixelBar.Sdk.csproj
```

## 快速开始

```csharp
using PixelBar.Sdk;
using PixelBar.Sdk.Protocol;

using var client = PixelBarSdk.ConnectPrimary();

client.ShowText("你好 PixelBar");
client.SetLight(LightMode.PureStatic, RgbColor.FromHex("#00FF00"));
client.ShowClock(3);
client.ShowSpectrum(1);
client.SetScreenColor(RgbColor.FromHex("#0077EE"));
```

## API 概览

| API | 说明 |
|-----|------|
| `ShowText` | 文字 + 布局（静态/滚动、对齐） |
| `SetLight` | 6 种灯效 + 颜色（速度仅 mode 1–2） |
| `ShowClock` / `ShowPattern` | 时钟样式 1–11 |
| `ShowSpectrum` | 频谱样式 1–4 |
| `SetScreenColor` | 像素屏主题色 |

完整文档：[src/PixelBar.Sdk/README.md](https://github.com/traceless929/PixelBar/blob/main/src/PixelBar.Sdk/README.md)

## 生态共建

欢迎基于 SDK 做歌词屏、神光同步桥接、Stream Deck 插件等。通用能力可通过 PR 合入 SDK。

路线图见 **[路线图](Roadmap)**。
