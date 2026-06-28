# Wiki 源文件

GitHub Wiki 的 Markdown 源文件存放在此目录，便于版本管理与 PR 协作。

## 页面

| 文件 | Wiki 页面 |
|------|-----------|
| `Home.md` | 首页 |
| `Roadmap.md` | 路线图 |
| `Downloads.md` | 下载与 Release |
| `SDK-Development.md` | SDK 开发 |
| `Features.md` | 功能支持范围 |
| `Reverse-Engineering-TempoHub.md` | 逆向 TempoHub |
| `Disclaimer.md` | 免责声明 |
| `_Sidebar.md` | 侧边栏导航 |

## 发布到 GitHub

首次使用前，请在 GitHub 仓库 **Settings → Features → Wikis** 中启用 Wiki，并手动创建任意一页（例如空白 Home），以初始化 `PixelBar.wiki` 仓库。

```powershell
./scripts/publish-wiki.ps1
```

脚本会 clone `PixelBar.wiki` 仓库、复制本目录内容并 push。修改 Wiki 后请同步更新此目录，保持仓库与线上 Wiki 一致。
