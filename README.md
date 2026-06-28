# 花再 Halo PixelBar 自定义控制

[![CI](https://github.com/traceless929/PixelBar/actions/workflows/ci.yml/badge.svg)](https://github.com/traceless929/PixelBar/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

逆向工程漫步者花再 **Halo PixelBar** 的 HID 协议，提供 **WinUI 桌面客户端**、**命令行工具** 与 **.NET SDK**。

**PixelBar.Sdk 面向开发者开放**：协议细节与校验和已由 SDK 封装，你无需重复抓包、拼包，可直接引用 API 做插件、自动化、桌面工具或与其他软件联动。欢迎基于 SDK 二次开发，共建 PixelBar 周边生态。

官方配套软件：

- **PC 端**：EDIFIER TempoHub（默认 `C:\Program Files (x86)\EDIFIER TempoHub`，可用环境变量 `TEMPOHUB_DIR` 覆盖）
- **手机端**：EDIFIER Connect

## 快速开始

### 图形客户端（推荐）

```bash
dotnet run --project src/PixelBar.App
```

打开 **设置** 选择设备后，即可使用文字、灯光、时钟、频谱、屏色等功能。详见 [`src/PixelBar.App/README.md`](src/PixelBar.App/README.md)。

### 命令行

```bash
dotnet run --project src/PixelBar.Cli -- text "你好 PixelBar"
dotnet run --project src/PixelBar.Cli -- screen-color "#0077EE"
dotnet run --project src/PixelBar.Cli -- spectrum 1
```

### SDK

```csharp
var client = PixelBarSdk.ConnectPrimary();
client.SetScreenColor(RgbColor.FromHex("#0077EE"));
```

## 设备信息

- **设备**: 漫步者花再 Halo PixelBar
- **VID/PID**: 0x2D99 / 0xA106
- **厂商**: 杰理科技 (Jieli Technology)
- **HID 接口**: MI_04 Col02 (UsagePage 0xFF24) — 屏幕/灯光数据通道

## 目录结构

```
src/
  PixelBar.Sdk/           C# SDK（NuGet 可打包，供二次开发引用）
  PixelBar.Cli/           命令行工具（dotnet global tool: pixelbar）
  PixelBar.App/           WinUI 3 图形客户端
legacy/python/            Python HID 脚本（遗留，见 legacy/python/README.md）
analysis/                 分析与 TempoHub 探测
hooks/                    Frida 抓包
capture/                  抓包日志（*.txt 已 gitignore）
.github/                  CI、Release、Issue/PR 模板
```

## SDK 与 CLI（二次开发）

本项目将已验证的 HID 协议沉淀为 **PixelBar.Sdk**，**开放给社区自由使用**（项目引用或 NuGet 打包均可）。二次开发者只需关心业务逻辑——例如歌词显示、RGB 联动、游戏 HUD——而不必再自行逆向 TempoHub、计算 checksum 或维护 64 字节报文格式。

我们期望出现更多样的上位机、脚本与集成方案，与官方 TempoHub / Connect 形成互补，推动 PixelBar 生态多元化发展。新能力若适合通用场景，也欢迎通过 PR 贡献回 SDK。

### 引用 SDK

```bash
# 项目引用
dotnet add YourApp.csproj reference src/PixelBar.Sdk/PixelBar.Sdk.csproj

# 或打包后本地安装
dotnet pack src/PixelBar.Sdk/PixelBar.Sdk.csproj -c Release -o ./nupkg
dotnet add package PixelBar.Sdk --source ./nupkg
```

```csharp
using PixelBar.Sdk;
using PixelBar.Sdk.Protocol;

var client = PixelBarSdk.ConnectPrimary();
client.ShowText("你好");
client.SetLight(LightMode.PureStatic, RgbColor.FromHex("#00FF00"));
client.ShowSpectrum(1);
```

详见 [`src/PixelBar.Sdk/README.md`](src/PixelBar.Sdk/README.md)。

### 安装 CLI

```bash
# 开发运行
dotnet run --project src/PixelBar.Cli -- text "你好世界"

# 发布单文件 exe（与 Release 相同）
dotnet publish src/PixelBar.Cli/PixelBar.Cli.csproj -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None -p:DebugSymbols=false -o publish/cli
# 输出: publish/cli/pixelbar.exe

pixelbar list
pixelbar text "你好 PixelBar"
pixelbar rgb 3 #00FF00
```

## 使用方法

### Python（遗留脚本）

```bash
pip install -r requirements-dev.txt
python legacy/python/pixel_text_send.py "你好世界"
python legacy/python/pixel_rgb_send.py 3 #00ff00
python legacy/python/pixel_img_send.py 1
```

### C# CLI（同 `pixelbar` 全局工具）

```bash
dotnet run --project src/PixelBar.Cli -- text "你好世界"
dotnet run --project src/PixelBar.Cli -- rgb 3 #00FF00
dotnet run --project src/PixelBar.Cli -- pattern 1
dotnet run --project src/PixelBar.Cli -- list
dotnet run --project src/PixelBar.Cli -- spectrum 1
```

### WinUI 3 客户端

Mica 背景 + 卡片式布局，侧栏实时显示设备连接状态。

```bash
dotnet run --project src/PixelBar.App
```

用户指南：[`src/PixelBar.App/README.md`](src/PixelBar.App/README.md) · 需要 Windows 10 1809+

## 协议格式

### 文字包 (Header: `2E AA EC E8`)

```
[2E AA EC E8] [total_hi] [total_lo] [00] [text_len] [UTF-8 text] [checksum] [00...]
```

- total = text_len + 2
- checksum = (sum(bytes[0:8+text_len]) + 0xD2) & 0xFF
- 总长 64 字节

**布局包**（须在文字包之前发送，Header `2E AA EC EF`，子命令 `F0 B4 C8 00 02`）：

| 效果 | 对齐 | byte12 | byte13 | 说明 |
|------|------|--------|--------|------|
| 静态 | 左 | 00 | 00 | |
| 静态 | 居中 | 00 | 01 | 默认 |
| 静态 | 右 | 00 | 02 | |
| 静态 | 两端 | 00 | 03 | |
| 滚动 | 左→右 | 01 | 00 | 动态显示 |
| 滚动 | 右→左 | 01 | 01 | |

```bash
dotnet run --project src/PixelBar.Cli -- text "Hello PixelBar" --align left
dotnet run --project src/PixelBar.Cli -- text "滚动文字" --scroll ltr
dotnet run --project src/PixelBar.Cli -- text "滚动文字" --scroll rtl
```

### RGB 灯光包 (Header: `2E AA EC 6B`)

```
[2E AA EC 6B] [00 07 13] [mode] [R] [G] [B] [0x3C] [speed] [checksum] [00...]
```

- mode: 1–6（氛围呼吸、幻彩潮汐、纯色静光、炫彩涟漪、流光逐影、动态光影）
- speed: 仅 **mode 1–2**（氛围呼吸、幻彩潮汐）有效，1 最慢、16 最快
- checksum = (sum(bytes[0:13]) + 0xD2) & 0xFF

### 图案包 (Header: `2E AA EC EF`)

```
[2E AA EC EF] [00 09 01 F0 B4 C8] [00 01] [index_hi] [index_lo] [FF] [cs_lo] [cs_hi] [00...]
```

- index = pattern - 1（big-endian，byte 12–13）
- checksum = (0xFFFB + index) & 0xFFFF（little-endian，byte 15–16）

## 支持范围

| 功能 | CLI / SDK | 桌面客户端 |
|------|-----------|------------|
| 文字 + 布局 | ✅ | ✅ |
| RGB 灯光 | ✅ | ✅ |
| 时钟 1–11 | ✅ | ✅ |
| 频谱 1–4 | ✅ | ✅ |
| 像素屏主题色 | ✅ | ✅ |
| 游戏/宠物/0x17 模板 | ❌ | ❌ |

**不支持的像素屏样式**（游戏、办公、阅读、宠物、表情包、赛博等，以及 TempoHub 新版 EF 时钟、自创空间图片）：须通过官方 **EDIFIER TempoHub / Connect** 上位机经 `0x17` 协议上传帧数据，本项目不实现该流程。

## 路线图

以下为计划中的功能，**尚未实现**；优先级与实现方式可能随逆向进展调整。欢迎 Issue / PR 参与；也鼓励开发者**直接基于 SDK** 先行实现原型（如歌词插件、神光同步桥接），成熟后再合入主线。

| 方向 | 目标 | 说明 |
|------|------|------|
| **RGB 神光同步** | 氛围灯与 PC 灯效生态联动 | 对接主板/外设 RGB 生态（如华硕 **Aura Sync（神光同步）**、微星 Mystic Light、技嘉 RGB Fusion 等），将系统/游戏/屏幕取色同步到 PixelBar 氛围灯，或与 TempoHub 现有灯效策略对齐 |
| **动态歌词** | 像素屏显示正在播放的歌词 | 接入常见音乐客户端，读取当前曲目与歌词进度并在 PixelBar 上滚动/逐字显示；优先调研 **QQ 音乐**、**网易云音乐** 等 Windows 版的窗口标题、媒体会话（SMTC）、插件或公开接口 |
| 协议补全 | 游戏/宠物/0x17 模板 | 依赖对 TempoHub `0x17` 帧上传流程的进一步逆向 |
| 体验优化 | 托盘、启动项、引导 | 持续迭代（部分已在桌面客户端落地） |

**动态歌词** 预期链路（草案）：音乐播放器 → 歌词与时间轴 → PixelBar 文字/布局协议 → 像素屏；需处理各播放器差异、无歌词 fallback、与 TempoHub 同时占用 HID 时的互斥等问题。

**神光同步** 预期链路（草案）：系统 RGB 源（Aura SDK / 屏幕取色 / 游戏钩子）→ 统一颜色与模式映射 → PixelBar RGB 灯光包（`0x6B`）；具体以各厂商 SDK 可用性与授权为准。

桌面客户端专项说明见 [`src/PixelBar.App/README.md`](src/PixelBar.App/README.md)。

### 个性场景 · 频谱类（已抓包验证）

切换样式 **只需 1 个 `0xEF` 包**（已在频谱 Tab 下时）：

```
[2E AA EC EF] [00 09 01] [C0 FF F2] [00 01] [08] [style 0~3] [FF] [cs_lo] [cs_hi]
  index = (categoryTab << 8) | styleIndex   → 频谱为 0x0800 ~ 0x0803
  checksum = (0x0040 + 8 + styleIndex) & 0xFFFF
```

| 样式 | 包（前 17 字节） |
|------|------------------|
| 1 柱状 | `… 08 00 ff 48` |
| 2 波浪 | `… 08 01 ff 49` |
| 3 对称 | `… 08 02 ff 4a` |
| 4 密集 | `… 08 03 ff 4b` |

```bash
dotnet run --project src/PixelBar.Cli -- spectrum 1
dotnet run --project src/PixelBar.Cli -- dry-run spectrum 2
```

### 像素屏颜色（已抓包验证）

TempoHub「像素屏颜色设置」：**单包 `0xEF`**，RGB 明文：

```
[2E AA EC EF] [00 04 03] [R] [G] [B] [cs_lo] [cs_hi]
  checksum = (0x008B + R + G + B - 255) & 0x01FF
```

| 颜色 | 包（前 12 字节） |
|------|------------------|
| #FF0000 | `… EF 00 04 03 FF 00 00 8B 00` |
| #00FF00 | `… EF 00 04 03 00 FF 00 8B 00` |
| #0000FF | `… EF 00 04 03 00 00 FF 8B 00` |
| #FFFFFF | `… EF 00 04 03 FF FF FF 89 00` |
| #0077EE | `… EF 00 04 03 00 77 EE F1 00` |

```bash
dotnet run --project src/PixelBar.Cli -- screen-color "#0077EE"
dotnet run --project src/PixelBar.Cli -- dry-run screen-color "#FF0000"
```

抓包：`python hooks/capture_screen_color.py` → `capture/screen_color_log.txt`

## 逆向官方 TempoHub

TempoHub 为 PyInstaller 打包的 Python 3.9 应用，HID 通信经 `hid.cp39-win_amd64.pyd`（`hid.device.write`）。

### 1. 检查安装与进程

```bash
python analysis/tempohub_info.py
```

若安装路径不同，复制 `analysis/tempohub_config_local.example.py` 为 `tempohub_config_local.py`，或设置环境变量 `TEMPOHUB_DIR`。

### 2. Frida 抓包

先启动 TempoHub，连接 PixelBar，在软件里操作像素屏/灯效：

```bash
pip install frida psutil
python hooks/capture_tempohub.py
```

或直接拉起 TempoHub：

```bash
python hooks/capture_tempohub.py --spawn
```

日志写入 `capture/usb_log.txt`。Hook 点：`WriteFile`、`hid_write`（hid.dll / hidapi / hid.pyd）。

### 3. 对比验证

将抓到的 64 字节包与 `dotnet run --project src/PixelBar.Cli -- dry-run ...` 输出对比，补充未知 opcode。

## 免责声明

本项目为**社区驱动的第三方开源项目**，与 **EDIFIER（漫步者）**、**杰理科技** 及官方 **TempoHub / EDIFIER Connect** **无任何隶属、授权或合作关系**。

- **商标与产品名称**：`EDIFIER`、`漫步者`、`花再`、`Halo`、`PixelBar`、`TempoHub`、`EDIFIER Connect` 等均为其各自权利人的商标或产品名称，本项目仅作识别用途，不暗示官方背书。
- **逆向与互操作性**：仓库中的协议说明、抓包脚本与 SDK 源于对**本人合法持有设备**的互操作性研究与社区分享，旨在减少重复劳动、便于二次开发；**不构成**对官方软件许可条款、用户协议或任何法律法规的解读或保证。
- **使用风险**：代码与文档按「**现状（AS IS）**」提供，**不提供**明示或暗示的保证。因使用本项目导致的设备异常、数据丢失、软件冲突、保修争议或任何直接或间接损失，**由使用者自行承担**；固件或官方上位机更新后，功能可能失效且**不保证**及时修复。
- **抓包与 Hook**：`hooks/`、`capture/` 等工具仅供在你**有权分析的环境**中调试本人设备；请勿用于破解授权、窃取商业机密、干扰他人系统或任何违法用途。分析 **TempoHub** 时请遵守其安装许可及当地法律。
- **官方渠道**：完整功能、固件升级与售后支持请优先使用官方 **EDIFIER TempoHub / Connect**。若官方要求停止相关逆向或分发，本项目维护者将**酌情**评估并响应。

使用本项目即表示你已阅读并理解上述内容；如有疑问，请咨询专业法律意见后再决定是否使用。

## 依赖

- Python 3.8+（`pip install -r requirements-dev.txt`，仅逆向抓包 / 图标生成需要）
- .NET 10 SDK（C# 项目，见 `global.json`）
- Windows

## 发布与 CI

- **CI**：推送到 `main` 或 PR 时自动 `dotnet build` 并打包 SDK（[`.github/workflows/ci.yml`](.github/workflows/ci.yml)）
- **Release**：推送标签 `v*.*.*` 时发布 GitHub Release，附件包含：
  - **`PixelBar.App-v{版本}-win-x64.exe`** — WinUI 图形客户端（单文件，约 120MB，自包含运行时）
  - **`pixelbar-v{版本}-win-x64.exe`** — CLI 命令行（单文件，约 70MB）
  - **`PixelBar.Sdk.{版本}.nupkg`** — SDK NuGet 包

  详见 [`.github/workflows/release.yml`](.github/workflows/release.yml)

```bash
git tag v1.0.0
git push origin v1.0.0
```

下载 Release 后 **双击 exe 即可运行**，无需单独安装 .NET。首次启动 WinUI 客户端若被杀软拦截，请允许运行。

本地打单文件 exe（与 CI 相同参数）：

```powershell
dotnet publish src/PixelBar.App/PixelBar.App.csproj -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableMsixTooling=true `
  -p:DebugType=None -p:DebugSymbols=false -o publish/app

dotnet publish src/PixelBar.Cli/PixelBar.Cli.csproj -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None -p:DebugSymbols=false -o publish/cli
```

贡献指南：[CONTRIBUTING.md](CONTRIBUTING.md)
