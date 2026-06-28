# 逆向 TempoHub

官方 PC 端 **EDIFIER TempoHub** 为 PyInstaller 打包的 Python 3.9 应用，HID 经 `hid.cp39-win_amd64.pyd`（`hid.device.write`）。

## 1. 检查安装

```bash
python analysis/tempohub_info.py
```

路径配置：复制 `analysis/tempohub_config_local.example.py` 为 `tempohub_config_local.py`，或设置 `TEMPOHUB_DIR`。

## 2. Frida 抓包

```bash
pip install -r requirements-dev.txt
python hooks/capture_tempohub.py
# 或
python hooks/capture_tempohub.py --spawn
```

日志：`capture/usb_log.txt`

## 3. 对比验证

将 64 字节包与 CLI dry-run 输出对比：

```bash
dotnet run --project src/PixelBar.Cli -- dry-run text "测试"
```

## 注意事项

抓包/Hooks 仅用于**合法持有设备**的互操作研究。详见 [免责声明](Disclaimer)。
