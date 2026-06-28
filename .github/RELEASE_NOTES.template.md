花再 **Halo PixelBar** 第三方控制工具。适用于 Windows 10/11 x64，USB 连接设备（VID/PID `0x2D99` / `0xA106`）。

## 我该下载哪个？

| 文件 | 适合谁 | 说明 |
|------|--------|------|
| **PixelBar.App-v{VERSION}-win-x64.exe** | 普通用户（推荐） | WinUI 图形客户端：文字、灯光、时钟、频谱、屏色、**QQ 音乐动态歌词**、托盘与开机启动。下载后**双击即可运行**，无需安装 .NET。 |
| **pixelbar-v{VERSION}-win-x64.exe** | 命令行 / 脚本用户 | CLI 工具，在 PowerShell 或 CMD 中使用，例如 `pixelbar text "你好"`、`pixelbar screen-color "#0077EE"`。 |
| **PixelBar.Sdk.{VERSION}.nupkg** | 开发者 | .NET SDK NuGet 包。可在 [GitHub Packages](https://github.com/traceless929/PixelBar/packages) 安装，或从此 Release 直接下载。 |
| **Source code (zip / tar.gz)** | 开发者 / 研究者 | GitHub 自动附带的对应标签源码归档，用于自行编译或阅读实现。 |

## v0.0.2 亮点

- **QQ 音乐动态歌词**：自动解密本地 qrc 缓存，按播放进度推送到像素屏
- 歌词时间偏移、长句滚动方向可调
- 解密缓存落盘，便于二次利用

详细说明：[Wiki · QQ 音乐动态歌词](https://github.com/traceless929/PixelBar/wiki/Lyrics-QQMusic)

## 使用提示

- 使用前请用 USB 连接 PixelBar，并在应用 **设置** 中选择设备。
- 使用动态歌词时请先**关闭 EDIFIER TempoHub**，并确保 QQ 音乐正在播放。
- 完整功能与固件升级仍请优先使用官方 TempoHub / Connect。

## 变更记录

https://github.com/traceless929/PixelBar/blob/main/CHANGELOG.md#002---2026-06-28

完整提交：https://github.com/traceless929/PixelBar/commits/v{VERSION}
