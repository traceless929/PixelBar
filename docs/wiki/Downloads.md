# 下载与 Release

## 最新版本

👉 [GitHub Releases（最新版）](https://github.com/traceless929/PixelBar/releases/latest)

## 我该下载哪个？

| 文件 | 适合谁 | 说明 |
|------|--------|------|
| **PixelBar.App-v{版本}-win-x64.exe** | 普通用户（推荐） | WinUI 图形客户端。文字、灯光、时钟、频谱、屏色、**QQ 音乐动态歌词**、托盘、开机启动。**双击运行**，无需安装 .NET。 |
| **pixelbar-v{版本}-win-x64.exe** | 命令行 / 脚本 | CLI，例如 `pixelbar text "你好"`、`pixelbar screen-color "#0077EE"`。 |
| **PixelBar.Sdk.{版本}.nupkg** | 开发者 | SDK NuGet 包；见 [GitHub Packages](https://github.com/traceless929/PixelBar/packages) 或 Release 附件。 |
| **Source code (zip / tar.gz)** | 开发者 | 对应标签的源码归档，自行编译用。 |

## GitHub Packages（SDK）

仓库侧边栏 **Packages** 提供 NuGet 源，Release 时会自动发布 `PixelBar.Sdk`。

安装说明见 **[SDK 开发](SDK-Development)**。

## 使用提示

- USB 连接 PixelBar，在 App **设置** 中选择设备。
- 若 **EDIFIER TempoHub** 正在占用 HID，请先关闭 TempoHub。
- 完整功能与固件升级请优先使用官方 TempoHub / Connect。

## 发版方式（维护者）

推送标签 `v*.*.*` 触发 [Release 工作流](https://github.com/traceless929/PixelBar/actions/workflows/release.yml)：

```bash
git tag v0.0.2
git push origin v0.0.2
```

自动产出：2 个单文件 exe、SDK nupkg（Release + Packages）。
