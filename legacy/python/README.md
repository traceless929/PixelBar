# Legacy Python HID scripts

早期用于协议验证的 Python 脚本，已由 **PixelBar.Sdk** / **PixelBar.Cli** 取代。仅作参考或快速调试。

```bash
pip install -r requirements-dev.txt
python legacy/python/pixel_text_send.py "Hello"
python legacy/python/pixel_rgb_send.py 3 "#00FF00"
python legacy/python/pixel_img_send.py 1
```

新开发请使用 [`PixelBar.Sdk`](../src/PixelBar.Sdk/README.md)。
