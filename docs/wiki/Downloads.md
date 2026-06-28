# 下载与 Release

## 最新版本

👉 [GitHub Releases（最新版）](https://github.com/traceless929/PixelBar/releases/latest)

## 我该下载哪个？

| 文件 | 适合谁 | 说明 |
|------|--------|------|
| **PixelBar.App-v{版本}-setup.exe** | 普通用户（推荐） | 标准安装包。含 Windows 版本 / x64 检测与安装引导，开始菜单快捷方式，支持卸载。 |
| **PixelBar.App-v{版本}-win-x64-portable.exe** | 免安装用户 | 单文件便携版，无需安装。若无法运行，右键 exe → 属性 → **解除锁定**。 |
| **pixelbar-v{版本}-win-x64.exe** | 命令行 / 脚本 | CLI，例如 `pixelbar text "你好"`、`pixelbar screen-color "#0077EE"`。 |
| **PixelBar.Sdk.{版本}.nupkg** | 开发者 | SDK NuGet 包；见 [GitHub Packages](https://github.com/traceless929/PixelBar/packages) 或 Release 附件。 |
| **Source code (zip / tar.gz)** | 开发者 | 对应标签的源码归档，自行编译用。 |

## 系统要求

- Windows 10 1809（build 17763）或更高，**64 位**
- USB 连接 Halo PixelBar（VID/PID `0x2D99` / `0xA106`）
- 无需单独安装 .NET（安装包与便携版均已内置运行时）

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
git tag v0.0.3.1
git push origin v0.0.3.1
```

自动产出：安装包 setup.exe、便携版 portable.exe、CLI exe、SDK nupkg。

本地构建安装包：`./scripts/build-installer.ps1`（需安装 [Inno Setup 6](https://jrsoftware.org/isdl.php)）。
