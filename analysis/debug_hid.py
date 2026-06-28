"""调试 PixelBar Col02 接口的友好名与报告长度。"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'legacy', 'python'))

import ctypes
from pixel_device import (
    FRIENDLY_NAME_HINT,
    REPORT_LENGTH,
    SP_DEVICE_INTERFACE_DATA,
    GUID_HID,
    setupapi,
    DIGCF_PRESENT,
    DIGCF_DEVICEINTERFACE,
    _get_interface_path,
    _read_friendly_name,
    _probe_device,
    _open_device,
)

hdevinfo = setupapi.SetupDiGetClassDevsW(
    ctypes.byref(GUID_HID), None, None, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE,
)
idx = 0
while True:
    iface = SP_DEVICE_INTERFACE_DATA()
    iface.cbSize = ctypes.sizeof(iface)
    if not setupapi.SetupDiEnumDeviceInterfaces(
        hdevinfo, None, ctypes.byref(GUID_HID), idx, ctypes.byref(iface),
    ):
        break
    idx += 1
    path, devinfo = _get_interface_path(hdevinfo, iface)
    if not path or 'col02' not in path.lower() or '2d99' not in path.lower():
        continue
    friendly = _read_friendly_name(hdevinfo, devinfo) if devinfo else ''
    dev = _probe_device(path, friendly)
    print('path:', path)
    print('repr:', repr(path))
    for fix in [path, '\\\\' + path]:
        try:
            h = _open_device(fix)
            print('OPEN OK', repr(fix[:50]))
        except OSError as e:
            print('OPEN FAIL', e.winerror, repr(fix[:50]))
    print('registry:', repr(friendly))
    if dev:
        print('merged:', repr(dev.friendly_name))
        print('input:', dev.input_report_length, 'expected:', REPORT_LENGTH)
        print('hint in merged:', FRIENDLY_NAME_HINT in dev.friendly_name)
        print('hint in registry:', FRIENDLY_NAME_HINT in friendly)

setupapi.SetupDiDestroyDeviceInfoList(hdevinfo)
