"""抓包 TempoHub 游戏类（个性场景 · 游戏 Tab）切换与 0x17 帧上传。

用法:
  python hooks/capture_game.py

在 TempoHub 中依次点击游戏样式 1–19，完成后 Ctrl+C 停止。
日志: capture/game_log.txt
解析: python analysis/parse_game_capture.py
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG = ROOT / "capture" / "game_log.txt"

STEPS = """
══════════════════════════════════════════════════════════
  TempoHub 游戏类抓包 — 请严格按顺序操作
══════════════════════════════════════════════════════════

前提: PixelBar 已连接，TempoHub 已打开并选中该设备。

1. 进入「像素屏」→「个性场景」
2. 点顶部分类「游戏」
   → 等 2 秒
3. 从样式 1 到 19 依次点击（每种等 2–3 秒，观察设备变化）
4. 可选：再点回样式 1 确认重复包
5. 回到本终端 Ctrl+C 结束抓包

抓完后运行:
  python analysis/parse_game_capture.py

══════════════════════════════════════════════════════════
"""


def main() -> int:
    parser = argparse.ArgumentParser(description="Capture TempoHub game scene HID packets")
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
