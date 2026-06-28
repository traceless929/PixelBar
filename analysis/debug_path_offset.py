"""探测 DevicePath 在 SetupAPI buffer 中的正确偏移。"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'legacy', 'python'))

import ctypes
from ctypes import wintypes
from pixel_device import (
    SP_DEVICE_INTERFACE_DATA,
    SP_DEVICE_INTERFACE_DETAIL_DATA_W,
    SP_DEVINFO_DATA,
    GUID_HID,
    setupapi,
    DIGCF_PRESENT,
    DIGCF_DEVICEINTERFACE,
    _DETAIL_CB_SIZE,
    _open_device,
)

hdevinfo = setupapi.SetupDiGetClassDevsW(
    ctypes.byref(GUID_HID), None, None, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE,
)
iface = SP_DEVICE_INTERFACE_DATA()
iface.cbSize = ctypes.sizeof(iface)
setupapi.SetupDiEnumDeviceInterfaces(hdevinfo, None, ctypes.byref(GUID_HID), 1, ctypes.byref(iface))

req_size = wintypes.DWORD()
devinfo = SP_DEVINFO_DATA()
devinfo.cbSize = ctypes.sizeof(devinfo)
setupapi.SetupDiGetDeviceInterfaceDetailW(
    hdevinfo, ctypes.byref(iface), None, 0, ctypes.byref(req_size), ctypes.byref(devinfo),
)
buf = ctypes.create_string_buffer(req_size.value)
detail_ptr = ctypes.cast(buf, ctypes.POINTER(SP_DEVICE_INTERFACE_DETAIL_DATA_W))
detail_ptr.contents.cbSize = _DETAIL_CB_SIZE
setupapi.SetupDiGetDeviceInterfaceDetailW(
    hdevinfo, ctypes.byref(iface), detail_ptr, req_size.value, None, ctypes.byref(devinfo),
)

from ctypes import wintypes

for off in range(0, 16, 2):
    s = ctypes.wstring_at(ctypes.addressof(buf) + off)
    if '2d99' in s.lower():
        try:
            _open_device(s)
            ok = 'OPEN OK'
        except OSError as e:
            ok = f'FAIL {e.winerror}'
        print(off, ok, repr(s[:55]))

setupapi.SetupDiDestroyDeviceInfoList(hdevinfo)
