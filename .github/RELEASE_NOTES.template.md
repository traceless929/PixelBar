花再 **Halo PixelBar** 第三方控制工具。适用于 Windows 10/11 x64，USB 连接设备（VID/PID `0x2D99` / `0xA106`）。



## 我该下载哪个？



| 文件 | 适合谁 | 说明 |

|------|--------|------|

| **PixelBar.App-v{VERSION}-setup.exe** | 普通用户（推荐） | 标准安装包：含系统环境检测与安装引导，写入开始菜单，支持卸载。 |

| **PixelBar.App-v{VERSION}-win-x64-portable.exe** | 免安装 / 高级用户 | 单文件便携版，下载后双击运行；若被 SmartScreen 拦截，请右键「属性 → 解除锁定」。 |

| **pixelbar-v{VERSION}-win-x64.exe** | 命令行 / 脚本用户 | CLI 工具，例如 `pixelbar text "你好"`、`pixelbar screen-color "#0077EE"`。 |

| **PixelBar.Sdk.{VERSION}.nupkg** | 开发者 | .NET SDK NuGet 包。可在 [GitHub Packages](https://github.com/traceless929/PixelBar/packages) 安装，或从此 Release 直接下载。 |

| **Source code (zip / tar.gz)** | 开发者 / 研究者 | GitHub 自动附带的对应标签源码归档，用于自行编译或阅读实现。 |



## v{VERSION} 亮点



- **标准安装包 + 便携版**：安装包含环境检测与使用引导；便携版免安装

- **修复 App 无法启动**：v0.0.2 / v0.0.2.1 单文件 exe 已不可用，请使用本版

- 含歌词 **重新同步** 与 QQ 音乐后台自动检测



详细说明：[Wiki · QQ 音乐动态歌词](https://github.com/traceless929/PixelBar/wiki/Lyrics-QQMusic)



## 使用提示



- 使用前请用 USB 连接 PixelBar，并在应用 **设置** 中选择设备。

- 使用动态歌词时请先**关闭 EDIFIER TempoHub**，并确保 QQ 音乐正在播放。

- 完整功能与固件升级仍请优先使用官方 TempoHub / Connect。



## 变更记录



https://github.com/traceless929/PixelBar/blob/main/CHANGELOG.md#003---2026-06-28



完整提交：https://github.com/traceless929/PixelBar/commits/v{VERSION}
