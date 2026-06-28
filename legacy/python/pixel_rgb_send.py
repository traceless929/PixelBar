import sys

from pixel_device import get_device_path, send_packet

MODES = {
    1: 'Ambient Breathing',
    2: 'Colorful Tide',
    3: 'Pure Static',
    4: 'Colorful Ripple',
    5: 'Flowing Light',
    6: 'Dynamic Shadow',
}


def build_rgb_packet(mode: int, r: int, g: int, b: int, brightness: int = 3, speed: int = 10) -> bytes:
    pkt = bytearray(64)
    pkt[0:4] = bytes.fromhex('2E AA EC 6B')
    pkt[4] = 0x00
    pkt[5] = 0x07
    pkt[6] = 0x13
    pkt[7] = mode & 0xFF
    pkt[8] = r & 0xFF
    pkt[9] = g & 0xFF
    pkt[10] = b & 0xFF
    pkt[11] = 0x3C
    pkt[12] = speed & 0xFF
    cs = (sum(pkt[:13]) + 0xD2) & 0xFF
    pkt[13] = cs
    return bytes(pkt)


def parse_color(s: str):
    s = s.lstrip('#')
    return int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16)


if __name__ == '__main__':
    usage = '''Usage:
  python pixel_rgb_send.py <mode> <color> [speed]
  python pixel_rgb_send.py 3 #00ff00        # pure static green (no speed needed)
  python pixel_rgb_send.py 1 #ff0000 16     # ambient breathing red, speed 16

Modes: 1=Ambient Breathing  2=Colorful Tide  3=Pure Static
       4=Colorful Ripple    5=Flowing Light  6=Dynamic Shadow

Speed: 1=slowest, 16=fastest (only applies to modes 1,2)
Brightness: fixed at 0x3C
'''
    if len(sys.argv) < 3:
        print(usage)
        sys.exit(1)

    mode = int(sys.argv[1])
    r, g, b = parse_color(sys.argv[2])
    speed = int(sys.argv[3]) if len(sys.argv) > 3 else 16

    if mode not in MODES:
        print(f'Invalid mode: {mode}. Valid: 1-6')
        sys.exit(1)

    path = get_device_path()
    pkt = build_rgb_packet(mode, r, g, b, speed=speed)
    send_packet(pkt, path)
    print(f'Sent: mode={MODES[mode]} color=#{r:02x}{g:02x}{b:02x} speed={speed}')
    print(f'device: {path}')
