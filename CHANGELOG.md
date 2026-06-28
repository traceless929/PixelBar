# 变更记录

本文件记录各版本的显著变更。完整提交历史见 GitHub。

## [0.0.3.1] - 2026-06-28

### 修复

- **修复 App 图标、Logo 与样式预览图不显示**：发布时将 `Assets` 复制到输出目录，并通过文件路径加载（unpackaged WinUI 不支持仅靠 `ms-appx:///`）

## [0.0.3] - 2026-06-28

### 新增

- **标准安装包**（Inno Setup）：`PixelBar.App-v{版本}-setup.exe`
  - 安装前检测 Windows 10 1809+ 与 x64 架构
  - 自定义安装引导页（TempoHub、USB 连接、设备选择说明）
  - 开始菜单 / 可选桌面快捷方式、卸载入口
- **便携版**单文件 exe：`PixelBar.App-v{版本}-win-x64-portable.exe`（免安装）

### 修复

- **修复 Release 版 PixelBar.App 无法启动**：关闭 `PublishTrimmed`（WinUI 不支持裁剪发布），便携版启用 `IncludeAllContentForSelfExtract`

## [0.0.2.1] - 2026-06-28

### 改进

- 歌词页新增 **重新同步** 按钮：清空 qrc 索引与当前曲目状态，强制重新匹配
- 开启推送后后台约每秒检测 QQ 音乐；先开 PixelBar 后开 QQ 音乐也会自动接上
- 等待状态文案改为「等待 QQ 音乐…（后台自动检测）」

## [0.0.2] - 2026-06-28

### 新增

- **QQ 音乐动态歌词**（桌面客户端）
  - 本地解密 `*_qm.qrc` 缓存，无需 LiLyric 等第三方工具
  - SMTC 播放进度同步；无歌名时按最近 qrc 文件名识别曲目
  - 解密结果落盘 `%LocalAppData%\PixelBar\LyricCache`（SHA256 去重）
  - 歌词时间偏移（毫秒）、长句滚动与滚动方向设置
  - QQ 音乐桌面歌词回退
- 歌词页 **使用引导** 与 Wiki 文档 [Lyrics-QQMusic](https://github.com/traceless929/PixelBar/wiki/Lyrics-QQMusic)
- `tools/QrcDecryptTest` 命令行解密验证工具

### 文档

- Wiki：路线图、功能范围、下载说明更新至 v0.0.2
- `THIRD_PARTY_NOTICES.md`：qrc 解密算法鸣谢

## [0.0.1] - 首版

- WinUI 桌面客户端：文字、灯光、时钟、频谱、屏色
- `PixelBar.Sdk` 与 `pixelbar` CLI
- 托盘、开机启动、首次使用引导
- GitHub Actions CI / Release 工作流

[0.0.3.1]: https://github.com/traceless929/PixelBar/compare/v0.0.3...v0.0.3.1
[0.0.3]: https://github.com/traceless929/PixelBar/compare/v0.0.2.1...v0.0.3
[0.0.2.1]: https://github.com/traceless929/PixelBar/compare/v0.0.2...v0.0.2.1
[0.0.2]: https://github.com/traceless929/PixelBar/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/traceless929/PixelBar/releases/tag/v0.0.1
