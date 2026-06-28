"""解析 capture/spectrum_log.txt，提取频谱相关 HID 包。"""

from __future__ import annotations

import re
import sys
from collections import OrderedDict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_LOG = ROOT / "capture" / "spectrum_log.txt"

HEX_RE = re.compile(r"len=64 \| ([0-9a-f]{128})", re.I)
OPCODE_RE = re.compile(r"^2eaaec([0-9a-f]{2})", re.I)


def opcode_label(op: str) -> str:
    return {
        "ef": "0xEF 场景",
        "77": "0x77 显示/灯",
        "e7": "0xE7 ?",
        "ee": "0xEE ?",
        "17": "0x17 帧上传",
        "6b": "0x6B RGB",
        "e8": "0xE8 文字",
    }.get(op.lower(), f"0x{op.upper()}")


def parse_ef_subtype(hexstr: str) -> str:
    if len(hexstr) < 24:
        return ""
    b6 = hexstr[12:14]
    if b6 == "02":
        tab = int(hexstr[24:26], 16)
        return f"EF·分类Tab={tab}"
    if b6 == "01":
        idx = int(hexstr[24:28], 16)
        return f"EF·样式index={idx}"
    return f"EF·byte6={b6}"


def main() -> int:
    log_path = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_LOG
    if not log_path.is_file():
        print(f"Log not found: {log_path}")
        print("Run: python hooks/capture_spectrum.py")
        return 1

    text = log_path.read_text(encoding="utf-8", errors="replace")
    unique: OrderedDict[str, None] = OrderedDict()
    for m in HEX_RE.finditer(text):
        unique.setdefault(m.group(1).lower(), None)

    if not unique:
        print(f"No 64-byte packets in {log_path}")
        return 1

    print(f"Spectrum capture: {log_path}")
    print(f"Unique packets: {len(unique)}\n")

    groups: dict[str, list[str]] = {}
    for pkt in unique:
        op = OPCODE_RE.match(pkt)
        key = opcode_label(op.group(1)) if op else "other"
        groups.setdefault(key, []).append(pkt)

    for label, packets in groups.items():
        print(f"── {label} ({len(packets)}) ──")
        for i, pkt in enumerate(packets, 1):
            spaced = " ".join(pkt[j : j + 2] for j in range(0, 34, 2))
            extra = parse_ef_subtype(pkt) if pkt.startswith("2eaaecef") else ""
            suffix = f"  [{extra}]" if extra else ""
            print(f"  {i:2}. {spaced}{suffix}")
        print()

    ef_packets = groups.get("0xEF 场景", [])
    if len(ef_packets) < 2:
        print("⚠ 仅抓到少量 0xEF 包，可能未完整操作 4 种频谱样式，请重抓。")
    else:
        print("请将上方 0xEF / 0x77 包与 TempoHub 操作步骤对照，更新 SceneReportEncoder。")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
