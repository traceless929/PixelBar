"""抓包 TempoHub 频谱类（个性场景 · 频谱 Tab）切换流程。

用法:
  python hooks/capture_spectrum.py

在 TempoHub 中按提示依次操作，完成后 Ctrl+C 停止。
日志: capture/spectrum_log.txt
解析: python analysis/parse_spectrum_capture.py
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG = ROOT / "capture" / "spectrum_log.txt"

STEPS = """
══════════════════════════════════════════════════════════
  TempoHub 频谱类抓包 — 请严格按顺序操作
══════════════════════════════════════════════════════════

前提: PixelBar 已连接，TempoHub 已打开并选中该设备。

1. 进入「像素屏」→ 左侧选「个性场景」
2. 点顶部分类「频谱」（第 9 个图标）
   → 等 2 秒
3. 依次点击 4 种频谱样式（对应 pixel_spectrum_0~3 预览图）:
   · 样式 1 柱状频谱
   · 样式 2 点阵波浪
   · 样式 3 中心对称波形
   · 样式 4 密集波形
   每点一种等 2 秒，观察设备是否变化
4. 再点回样式 1，确认重复包一致
5. Ctrl+C 结束抓包

抓完后运行:
  python analysis/parse_spectrum_capture.py

══════════════════════════════════════════════════════════
"""


def main() -> int:
    parser = argparse.ArgumentParser(description="Capture TempoHub spectrum scene HID packets")
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
