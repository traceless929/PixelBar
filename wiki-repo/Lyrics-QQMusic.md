# QQ 音乐动态歌词

PixelBar **v0.0.2+** 桌面客户端支持在像素屏上同步显示 **QQ 音乐** 歌词，无需第三方歌词工具。

## 功能概览

| 能力 | 说明 |
|------|------|
| 本地 qrc 解密 | 自动解密 QQ 音乐缓存 `*_qm.qrc`（自研算法，见 [THIRD_PARTY_NOTICES](https://github.com/traceless929/PixelBar/blob/main/THIRD_PARTY_NOTICES.md)） |
| 进度同步 | 通过 Windows SMTC 读取 QQ 音乐播放进度 |
| 曲目识别 | SMTC 无歌名时，按最近修改的 qrc 文件名匹配 |
| 解密缓存 | 明文 XML 落盘 `%LocalAppData%\PixelBar\LyricCache`，按 SHA256 去重 |
| 时间偏移 | 可微调歌词相对文件时间轴的早晚（毫秒） |
| 长句滚动 | 超出屏宽时可滚动，支持左→右 / 右→左 |
| 桌面歌词回退 | 读 QQ 音乐桌面歌词窗口作为备选 |

## 快速开始

### 1. 前置条件

- Windows 10/11，USB 连接 PixelBar
- 在 PixelBar **设置** 中已选择设备（侧栏显示「已连接」）
- **关闭 EDIFIER TempoHub**（避免 HID 冲突）
- 已安装 **QQ 音乐** PC 客户端

### 2. 准备歌词缓存

1. 用 QQ 音乐播放目标歌曲（建议完整播放一遍）
2. QQ 音乐会在缓存目录写入加密歌词，通常为：

   ```
   {CACHEPATH}\QQMusicLyricNew\{歌手} - {歌名} - {码率} - {专辑}_qm.qrc
   ```

3. 缓存根路径默认读注册表 `HKCU\Software\Tencent\QQMusic\LogConfig\CACHEPATH`  
   也可在 App **动态歌词 → 高级选项** 中手动指定

### 3. 开启推送

1. 打开 PixelBar **动态歌词 · QQ 音乐**
2. 开启 **启用歌词推送**
3. 运行状态应变为 **正在推送歌词**，并显示当前行

### 4. 微调（可选）

| 设置 | 说明 |
|------|------|
| **歌词时间偏移** | 正数 = 晚一些出现；负数 = 早一些。建议从 ±200～500 ms 试起 |
| **长句滚动显示** | 超长歌词在屏上滚动而非截断 |
| **滚动方向** | 左→右 或 右→左 |

## 工作原理

```
QQ 音乐播放
  → SMTC 进度 + 曲目识别（歌名 / 最近 qrc 文件名）
  → 读取 QQMusicLyricNew/*_qm.qrc
  → SHA256 查本地 LyricCache，未命中则解密并写入
  → 解析 QRC 时间轴，按偏移取当前行
  → PixelBar 文字协议推送到像素屏
```

## 目录与缓存

| 路径 | 内容 |
|------|------|
| `{QQ CACHEPATH}\QQMusicLyricNew\` | QQ 音乐加密的 `*_qm.qrc` |
| `%LocalAppData%\PixelBar\LyricCache\{hash}.xml` | 解密后的明文 QRC XML |
| `%LocalAppData%\PixelBar\LyricCache\{hash}.meta.json` | 源文件名、歌名、歌手等元数据 |
| `%LocalAppData%\PixelBar\settings.json` | 歌词相关设置持久化 |

解密后的 XML 可直接给其它工具二次利用。

## 故障排除

| 现象 | 建议 |
|------|------|
| 一直「等待 QQ 音乐播放」 | 确认 QQ 音乐正在播放；检查是否被其它软件占用 SMTC |
| 「QQ qrc 0 首」 | 先播放歌曲生成缓存；检查高级选项中的缓存目录 |
| 有进度但只显示歌名 | qrc 解密失败或尚未缓存；可开 QQ 音乐桌面歌词作回退 |
| 歌词与进度不同步 | 调整 **时间偏移**；确认 QQ 音乐未大幅变速 |
| 推送失败 / 无反应 | 关闭 TempoHub；在设置中重新选择设备 |
| 与 LiLyric 等冲突 | PixelBar 独立链路，无需 LiLyric；但 HID 仍只能一个程序占用 |

## 开发者

- 解密实现：`src/PixelBar.App/Services/Lyrics/QmQrcDecoder.cs`
- 同步服务：`LyricsSyncService.cs`
- 命令行验证：`dotnet run --project tools/QrcDecryptTest -- "D:\QQMusicCache\QQMusicLyricNew"`

## 后续计划

- 网易云音乐等其它播放器
- 更精细的字级 KTV 歌词（当前为行级）

详见 [路线图](Roadmap)。
