# Contributing to PixelBar

感谢参与 **PixelBar**（花再 Halo PixelBar 第三方控制生态）！

## 开发环境

- Windows 10+
- [.NET 10 SDK](https://dotnet.microsoft.com/download)（见 `global.json`）
- （可选）Python 3.8+：`pip install -r requirements-dev.txt`（逆向抓包 / 图标生成）

```bash
git clone https://github.com/traceless929/PixelBar.git
cd PixelBar
dotnet build PixelBar.slnx -c Release
dotnet run --project src/PixelBar.App
```

## 提交规范

- 分支：`feature/...`、`fix/...` 从 `main` 拉出
- PR 请使用 [.github/PULL_REQUEST_TEMPLATE.md](.github/PULL_REQUEST_TEMPLATE.md)
- 保持改动聚焦；协议变更请附 `dry-run` 输出或抓包说明
- 勿提交 `bin/`、`obj/`、`capture/*.txt`、本地 TempoHub 路径配置（见 `analysis/tempohub_config_local.example.py`）

## SDK 贡献

通用 API 优先加到 [`PixelBar.Sdk`](src/PixelBar.Sdk/)；CLI 与 WinUI 为消费方。

## 法律与商标

见 [README 免责声明](README.md#免责声明)。本项目与 EDIFIER 官方无隶属关系。
