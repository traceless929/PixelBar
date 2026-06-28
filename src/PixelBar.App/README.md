# PixelBar 桌面客户端

面向 **漫步者花再 Halo PixelBar** 的 WinUI 3 图形控制面板，基于 [`PixelBar.Sdk`](../PixelBar.Sdk/README.md) 构建。

## 功能概览

| 模块 | 说明 |
|------|------|
| 文字 | 静态/滚动显示，对齐方式可选 |
| 灯光 | 6 种氛围灯效 + 颜色 + 速度 |
| 像素屏 · 时钟 | 11 种固件内置样式 |
| 像素屏 · 频谱 | 4 种实时频谱样式 |
| 像素屏 · 颜色 | 主题色调（HEX/RGB，已抓包验证） |
| 设置 | 设备选择、连接状态、开机启动、托盘行为 |

## 系统要求

- Windows 10 1809+（推荐 Windows 11）
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- USB 连接的 Halo PixelBar（VID/PID `0x2D99` / `0xA106`）

应用图标：`src/PixelBar.App/Assets/`（源文件 `assets/pixelbar-logo-master.png`）。重新生成：

```bash
python analysis/generate_app_icons.py
```

## 运行

```bash
# 开发模式
dotnet run --project src/PixelBar.App

# 发布独立程序
dotnet publish src/PixelBar.App/PixelBar.App.csproj -c Release -r win-x64 --self-contained
# 输出目录: bin/Release/net10.0-windows10.0.26100.0/win-x64/publish/
```

项目为 **非打包（unpackaged）** 模式，可直接 `dotnet run`，无需开启开发者模式。

## 使用指南

### 首次启动

应用会显示**使用引导**，说明连接设备与功能概览。完成后可在 **设置 → 查看使用引导** 再次打开。

### 1. 连接设备

1. 用 USB 连接 PixelBar
2. 打开应用，点击左侧底部 **设置**
3. 点击 **刷新**，在列表中选择设备
4. 侧栏顶部应显示绿色 **已连接** 状态

### 3. 应用行为（设置 → 应用行为）

- **开机自动启动**：写入当前用户注册表 `Run` 项，登录后自动启动
- **关闭时最小化到通知区域**：点关闭按钮隐藏到托盘；双击托盘图标或选「显示主窗口」恢复；选「退出 PixelBar」完全关闭

若同时开启两项，开机启动时会带 `--tray` 参数，默认只显示托盘图标不弹出窗口。

设置保存在 `%LocalAppData%\PixelBar\settings.json`。

### 4. 文字显示

**文字** 页面输入内容（建议静态 ≤16 字），选择效果与对齐，点击 **发送到屏幕**。

### 5. 灯光

**灯光** 页面选择模式与颜色。速度滑块仅对「氛围呼吸」「幻彩潮汐」生效。

### 6. 像素屏

- **时钟** / **频谱**：点击卡片即可切换
- **颜色**：色盘或快捷预设，点击 **应用颜色**

## 界面结构

```
PixelBar
├── 文字
├── 灯光
├── 像素屏
│   ├── 时钟
│   ├── 频谱
│   └── 颜色
└── 设置
```

## 架构

```
PixelBar.App/
  Controls/       PageHeader 等共享控件
  Helpers/        UiFeedback 等
  Pages/          各功能页面
  Services/       PixelBarService（设备单例）
```

所有 HID 操作经 `PixelBarService.CreateClient()` 获取 `PixelBarClient` 实例。

## 不支持的功能

以下须使用官方 **EDIFIER TempoHub / Connect**：

- 游戏、办公、宠物等个性场景模板
- 自创空间图片/动画（`0x17` 帧上传）
- 热点新闻、天气预报

计划中的功能（RGB 神光同步、QQ 音乐/网易云等动态歌词等）见 [项目路线图](../../README.md#路线图)。相关扩展建议优先基于 [`PixelBar.Sdk`](../PixelBar.Sdk/README.md) 实现，无需自行处理协议层。

## 故障排除

| 现象 | 建议 |
|------|------|
| 设置中找不到设备 | 检查 USB、重新插拔；关闭 TempoHub 后重试 |
| 发送失败 | 确认侧栏显示「已连接」；到设置重新选择设备 |
| 构建时 DLL 被锁定 | 关闭正在运行的 PixelBar.App 后重新编译 |

## 相关文档

- [项目总览 / 协议说明](../../README.md)
- [免责声明](../../README.md#免责声明)
- [SDK 开发文档](../PixelBar.Sdk/README.md)
- [CLI 命令行](../PixelBar.Cli/Program.cs)（`pixelbar` 工具）
