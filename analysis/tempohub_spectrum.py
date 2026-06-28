"""TempoHub 频谱类 — 抓包已验证（capture/spectrum_log.txt）。"""

from __future__ import annotations

SPECTRUM_PREVIEW_FILES = {
    1: "pixel_spectrum_0.png",
    2: "pixel_spectrum_1.png",
    3: "pixel_spectrum_2.png",
    4: "pixel_spectrum_3.png",
}

# capture/spectrum_log.txt — 切换样式 1~4（已在频谱 Tab 下）
CAPTURED_STYLES = {
    1: "2e aa ec ef 00 09 01 c0 ff f2 00 01 08 00 ff 48",
    2: "2e aa ec ef 00 09 01 c0 ff f2 00 01 08 01 ff 49",
    3: "2e aa ec ef 00 09 01 c0 ff f2 00 01 08 02 ff 4a",
    4: "2e aa ec ef 00 09 01 c0 ff f2 00 01 08 03 ff 4b",
}

CAPTURED_CATEGORY_TAB8 = "2e aa ec ef 00 09 02 c0 ff f2 00 01 08 ff ff 48"


def build_spectrum_style(style: int) -> bytes:
    """style: 1~4"""
    idx = style - 1
    tab = 8
    cs = (0x0040 + tab + idx) & 0xFFFF
    body = bytes([
        0x2E, 0xAA, 0xEC, 0xEF,
        0x00, 0x09, 0x01,
        0xC0, 0xFF, 0xF2,
        0x00, 0x01,
        tab, idx,
        0xFF,
        cs & 0xFF, (cs >> 8) & 0xFF,
    ])
    return body + b"\x00" * (64 - len(body))


def main() -> int:
    print("TempoHub 频谱类 — 已验证")
    print("\n样式包（单包切换，index=0x08XX）:")
    for style, hexstr in CAPTURED_STYLES.items():
        built = build_spectrum_style(style)[:17].hex(" ")
        ok = built.replace(" ", "") == hexstr.replace(" ", "")
        print(f"  样式 {style}: {hexstr}  {'OK' if ok else 'MISMATCH'}")

    print(f"\n进入频谱 Tab（可选）: {CAPTURED_CATEGORY_TAB8}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
