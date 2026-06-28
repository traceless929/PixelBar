"""枚举本机 HID 设备（PixelBar 相关接口）。"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'legacy', 'python'))

from pixel_device import print_devices

if __name__ == '__main__':
    print_devices()
