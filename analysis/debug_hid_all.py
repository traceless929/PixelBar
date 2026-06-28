"""列出本机全部 HID 设备，筛选可能相关的接口。"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'legacy', 'python'))

import ctypes
from pixel_device import (
    SP_DEVICE_INTERFACE_DATA,
    GUID_HID,
    setupapi,
    DIGCF_PRESENT,
    DIGCF_DEVICEINTERFACE,
    _get_interface_path,
    _read_friendly_name,
    _probe_device,
)

KEYWORDS = ('pixel', 'halo', '花再', 'edifier', '2d99', 'a106', '漫步者')

hdevinfo = setupapi.SetupDiGetClassDevsW(
    ctypes.byref(GUID_HID), None, None, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE,
)
idx = 0
total = 0
matched = 0
while True:
    iface = SP_DEVICE_INTERFACE_DATA()
    iface.cbSize = ctypes.sizeof(iface)
    if not setupapi.SetupDiEnumDeviceInterfaces(
        hdevinfo, None, ctypes.byref(GUID_HID), idx, ctypes.byref(iface),
    ):
        break
    idx += 1
    path, devinfo = _get_interface_path(hdevinfo, iface)
    if not path:
        continue
    total += 1
    friendly = _read_friendly_name(hdevinfo, devinfo) if devinfo else ''
    dev = _probe_device(path, friendly)
    blob = f'{path} {friendly} {dev.friendly_name if dev else ""}'.lower()
    if any(k in blob for k in KEYWORDS):
        matched += 1
        print('---')
        print('path:', path)
        print('friendly:', repr(friendly))
        if dev:
            print('name:', repr(dev.friendly_name))
            print('in/out:', dev.input_report_length, dev.output_report_length)
            print('usage:', hex(dev.usage_page), hex(dev.usage))

setupapi.SetupDiDestroyDeviceInfoList(hdevinfo)
print(f'total HID: {total}, keyword matches: {matched}')
