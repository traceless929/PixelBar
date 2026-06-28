"""解析 capture/game_log.txt，提取游戏类 0xEF / 0x17 包。"""

from __future__ import annotations

import re
import sys
from collections import OrderedDict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG = ROOT / "capture" / "game_log.txt"

HEX_RE = re.compile(r"len=64 \| ([0-9a-f]{128})", re.I)
OPCODE_RE = re.compile(r"^2eaaec([0-9a-f]{2})", re.I)


def opcode_label(op: str) -> str:
    return {
        "ef": "0xEF 场景",
        "17": "0x17 帧上传",
        "77": "0x77 显示/灯",
        "ee": "0xEE 前置",
        "6a": "0x6A 前置",
        "e7": "0xE7 ?",
        "e8": "0xE8 文字",
    }.get(op.lower(), f"0x{op.upper()}")


def parse_ef(hexstr: str) -> str:
    if len(hexstr) < 28:
        return ""
    b6 = hexstr[12:14]
    if b6 == "02":
        tab = int(hexstr[24:26], 16)
        return f"EF·Tab={tab}"
    if b6 == "01":
        idx = int(hexstr[24:28], 16)
        return f"EF·index=0x{idx:04X}"
    if hexstr[14:20] == "f0b4c8":
        idx = int(hexstr[24:28], 16)
        return f"EF·legacy index={idx}"
    return f"EF·byte6={b6}"


def parse_17(hexstr: str) -> str:
    if len(hexstr) < 20:
        return ""
    length = int(hexstr[8:12], 16)
    if length == 0x0006:
        return "17·头包"
    if length == 0x0036:
        seq = int(hexstr[16:18], 16) if len(hexstr) >= 18 else -1
        return f"17·数据 seq={seq}"
    return f"17·len=0x{length:04X}"


def main() -> int:
    log_path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_LOG
    if not log_path.is_file():
        print(f"Log not found: {log_path}")
        return 1

    text = log_path.read_text(encoding="utf-8", errors="replace")
    packets: list[str] = []
    seen: set[str] = set()
    for m in HEX_RE.finditer(text):
        pkt = m.group(1).lower()
        packets.append(pkt)
        seen.add(pkt)

    print(f"Game capture: {log_path}")
    print(f"Total writes: {len(packets)}, unique: {len(seen)}\n")

    groups: dict[str, list[str]] = {}
    for pkt in packets:
        op = OPCODE_RE.match(pkt)
        key = opcode_label(op.group(1)) if op else "other"
        groups.setdefault(key, []).append(pkt)

    for label, items in groups.items():
        print(f"── {label} ({len(items)} writes, {len(set(items))} unique) ──")
        for i, pkt in enumerate(dict.fromkeys(items), 1):
            spaced = " ".join(pkt[j : j + 2] for j in range(0, min(34, len(pkt)), 2))
            if pkt.startswith("2eaaecef"):
                extra = parse_ef(pkt)
            elif pkt.startswith("2eaaec17"):
                extra = parse_17(pkt)
            else:
                extra = ""
            suffix = f"  [{extra}]" if extra else ""
            print(f"  {i:2}. {spaced}{suffix}")
        print()

    ef_styles = sorted(
        {int(pkt[24:28], 16) for pkt in groups.get("0xEF 场景", []) if pkt[12:14] == "01" and pkt[14:20] == "c0fff2"}
    )
    if ef_styles:
        print("EF 样式 index:", ", ".join(f"0x{x:04X}" for x in ef_styles))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
