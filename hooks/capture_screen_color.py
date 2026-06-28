"""抓包 TempoHub「像素屏颜色设置」操作。

用法:
  python hooks/capture_screen_color.py

在 TempoHub 中按提示依次操作，完成后 Ctrl+C 停止。
日志: capture/screen_color_log.txt
解析: python analysis/parse_screen_color_capture.py
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG = ROOT / "capture" / "screen_color_log.txt"

STEPS = """
══════════════════════════════════════════════════════════
  TempoHub 像素屏颜色设置抓包 — 请严格按顺序操作
══════════════════════════════════════════════════════════

前提: PixelBar 已连接，TempoHub 已打开并选中该设备。

1. 进入「像素屏」Tab，确保像素屏开关为 ON
2. 点击左下角「像素屏颜色设置」，打开颜色面板
3. 依次在 HEX 输入框设置并确认（每步等 2 秒，观察设备色调变化）:
   · #FF0000  (纯红)
   · #00FF00  (纯绿)
   · #0000FF  (纯蓝)
   · #FFFFFF  (白)
   · #0077EE  (截图中的蓝)
4. 依次点击右上角「主题颜色」预设圆点 1–8（每点等 2 秒）
5. 点击「同步氛围灯颜色」按钮一次
6. Ctrl+C 结束抓包

抓完后运行:
  python analysis/parse_screen_color_capture.py

══════════════════════════════════════════════════════════
"""


def main() -> int:
    parser = argparse.ArgumentParser(description="Capture TempoHub pixel screen color HID packets")
    parser.add_argument("--spawn", action="store_true", help="Launch TempoHub via capture_tempohub.py --spawn")
    parser.add_argument("--log", type=Path, default=DEFAULT_LOG, help="Output log path")
    args = parser.parse_args()

    print(STEPS)

    if args.log.exists():
        args.log.unlink()
        print(f"Cleared old log: {args.log}\n")

    cmd = [
        sys.executable,
        str(ROOT / "hooks" / "capture_tempohub.py"),
        "--filter",
        "packet",
        "--log",
        str(args.log),
    ]
    if args.spawn:
        cmd.append("--spawn")

    print(f"Starting capture → {args.log}\n")
    return subprocess.call(cmd)


if __name__ == "__main__":
    raise SystemExit(main())
