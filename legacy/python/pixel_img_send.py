import sys

from pixel_device import get_device_path, send_packet

CAPTURED = {
    1:  '2e aa ec ef 00 09 01 f0 b4 c8 00 01 00 00 ff fb',
    2:  '2e aa ec ef 00 09 01 f0 b4 c8 00 01 00 01 ff fc',
    3:  '2e aa ec ef 00 09 01 f0 b4 c8 00 01 00 02 ff fd',
    4:  '2e aa ec ef 00 09 01 f0 b4 c8 00 01 00 03 ff fe',
    5:  '2e aa ec ef 00 09 01 f0 b4 c8 00 01 00 04 ff ff',
    6:  '2e aa ec ef 00 09 01 f0 b4 c8 00 01 00 05 ff 00',
    7:  '2e aa ec ef 00 09 01 f0 b4 c8 00 01 00 06 ff 01',
    8:  '2e aa ec ef 00 09 01 f0 b4 c8 00 01 00 07 ff 02',
    9:  '2e aa ec ef 00 09 01 f0 b4 c8 00 01 00 08 ff 03',
    10: '2e aa ec ef 00 09 01 f0 b4 c8 00 01 00 09 ff 04',
    11: '2e aa ec ef 00 09 01 f0 b4 c8 00 01 00 0a ff 05',
}


def build_image_packet(pattern: int) -> bytes:
    index = pattern - 1
    pkt = bytearray(64)
    pkt[0:4] = bytes.fromhex('2E AA EC EF')
    pkt[4:10] = bytes.fromhex('00 09 01 F0 B4 C8')
    pkt[10:12] = bytes.fromhex('00 01')
    pkt[12] = (index >> 8) & 0xFF
    pkt[13] = index & 0xFF
    pkt[14] = 0xFF
    cs = (0xFFFB + index) & 0xFFFF
    pkt[15] = cs & 0xFF
    pkt[16] = (cs >> 8) & 0xFF
    return bytes(pkt)


if __name__ == '__main__':
    usage = '''Usage:
  python pixel_img_send.py <pattern>

  pattern: 1-11 (clock patterns)

Examples:
  python pixel_img_send.py 1    # clock pattern 1
  python pixel_img_send.py 11   # clock pattern 11
'''
    if len(sys.argv) < 2:
        print(usage)
        sys.exit(1)

    pattern = int(sys.argv[1])

    if pattern in CAPTURED:
        hexstr = CAPTURED[pattern]
        pkt = bytes.fromhex(hexstr) + b'\x00' * (64 - len(bytes.fromhex(hexstr)))
        print(f'Sending captured pattern {pattern}')
    elif 1 <= pattern <= 11:
        pkt = build_image_packet(pattern)
        print(f'Sending constructed pattern {pattern}')
    else:
        print(f'Invalid pattern: {pattern}. Valid: 1-11')
        sys.exit(1)

    path = get_device_path()
    send_packet(pkt, path)
    print('Done')
    print(f'device: {path}')
