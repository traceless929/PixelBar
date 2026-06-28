import time

from pixel_device import get_device_path, send_packet


def build_text_packet(text: str) -> bytes:
    data = text.encode('utf-8')
    if len(data) > 54:
        raise ValueError('文本 UTF-8 字节数不能超过 54')

    total = len(data) + 2
    pkt = bytearray(64)
    pkt[0:4] = bytes.fromhex('2E AA EC E8')
    pkt[4] = (total >> 8) & 0xFF
    pkt[5] = total & 0xFF
    pkt[6] = 0x00
    pkt[7] = len(data) & 0xFF
    pkt[8:8 + len(data)] = data

    cs = (sum(pkt[:8 + len(data)]) + 0xD2) & 0xFF
    pkt[8 + len(data)] = cs
    return bytes(pkt)


if __name__ == '__main__':
    import sys

    text = sys.argv[1] if len(sys.argv) > 1 else 'Hello PixelBar'
    path = get_device_path()
    pkt = build_text_packet(text)
    send_packet(pkt, path)
    print(f'sent: {text}')
    print(f'device: {path}')
    time.sleep(0.3)
