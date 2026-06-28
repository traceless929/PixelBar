"""解析 capture/screen_color_log.txt，提取像素屏颜色相关 HID 包。"""

from __future__ import annotations

import re
import sys
from collections import OrderedDict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG = ROOT / "capture" / "screen_color_log.txt"

HEX_RE = re.compile(r"len=64 \| ([0-9a-f]{128})", re.I)
OPCODE_RE = re.compile(r"^2eaaec([0-9a-f]{2})", re.I)


def opcode_label(op: str) -> str:
    return {
        "77": "0x77 显示/灯",
        "ee": "0xEE 应用",
        "6a": "0x6A 提交",
        "e7": "0xE7 ?",
        "6b": "0x6B RGB 灯",
        "ef": "0xEF 场景/颜色",
    }.get(op.lower(), f"0x{op.upper()}")


def parse_ef_subtype(hexstr: str) -> str:
    if len(hexstr) < 24:
        return ""
    if hexstr.startswith("2eaaecef000403"):
        r, g, b = int(hexstr[14:16], 16), int(hexstr[16:18], 16), int(hexstr[18:20], 16)
        cs = int(hexstr[20:24], 16)
        expected = (0x008B + r + g + b - 255) & 0x01FF
        ok = "OK" if cs == expected else f"expected {expected:04x}"
        return f"EF·屏色 #{r:02x}{g:02x}{b:02x} cs={cs:04x} ({ok})"
    if hexstr.startswith("2eaaecef"):
        return f"EF·prefix={hexstr[8:14]}"
    return ""


def parse_77_subtype(hexstr: str) -> str:
    if len(hexstr) < 34:
        return ""
    sub = hexstr[20:22]
    if sub == "11":
        b11, b12, b15 = hexstr[22:24], hexstr[24:26], hexstr[30:32]
        return f"77·屏色预览 sub=11 c1={b11} c2={b12} cs={b15}"
    if sub == "10":
        return "77·氛围 sub=10"
    return f"77·sub={sub}"


def main() -> int:
    log_path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_LOG
    if not log_path.is_file():
        print(f"Log not found: {log_path}")
        print("Run: python hooks/capture_screen_color.py")
        return 1

    text = log_path.read_text(encoding="utf-8", errors="replace")
    unique: OrderedDict[str, None] = OrderedDict()
    for m in HEX_RE.finditer(text):
        unique.setdefault(m.group(1).lower(), None)

    if not unique:
        print(f"No 64-byte packets in {log_path}")
        return 1

    print(f"Screen color capture: {log_path}")
    print(f"Unique packets: {len(unique)}\n")

    groups: dict[str, list[str]] = {}
    for pkt in unique:
        op = OPCODE_RE.match(pkt)
        key = opcode_label(op.group(1)) if op else "other"
        groups.setdefault(key, []).append(pkt)

    for label, packets in groups.items():
        print(f"-- {label} ({len(packets)}) --")
        for i, pkt in enumerate(packets, 1):
            spaced = " ".join(pkt[j : j + 2] for j in range(0, 34, 2))
            extra = parse_ef_subtype(pkt) or (parse_77_subtype(pkt) if pkt.startswith("2eaaec77") else "")
            suffix = f"  [{extra}]" if extra else ""
            print(f"  {i:2}. {spaced}{suffix}")
        print()

    screen_ef = [p for p in unique if p.startswith("2eaaecef000403")]
    if len(screen_ef) >= 3:
        print(f"OK: {len(screen_ef)} verified 0xEF 00 04 03 screen-color packets.")
    else:
        print("WARN: few 0xEF 00 04 03 packets; retry capture with #FF0000/#00FF00/#0000FF.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
