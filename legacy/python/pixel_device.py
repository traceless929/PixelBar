"""花再 Halo PixelBar HID 设备发现与发送。

按 VID/PID 匹配设备，筛选 Col02 屏幕数据通道（64 字节 HID 报告）。
"""
import ctypes
from ctypes import wintypes
from dataclasses import dataclass

PIXELBAR_VID = 0x2D99
PIXELBAR_PID = 0xA106
PIXELBAR_USAGE_PAGE = 0xFF24
REPORT_LENGTH = 64
FRIENDLY_NAME_HINT = '花再 Halo PixelBar'
# SetupDiGetDeviceInterfaceDetailW 要求的 cbSize（不是结构体 sizeof）
_DETAIL_CB_SIZE = 8 if ctypes.sizeof(ctypes.c_void_p) == 8 else 6

GENERIC_READ = 0x80000000
GENERIC_WRITE = 0x40000000
FILE_SHARE_READ = 1
FILE_SHARE_WRITE = 2
OPEN_EXISTING = 3
FILE_ATTRIBUTE_NORMAL = 0x80

setupapi = ctypes.windll.setupapi
kernel32 = ctypes.windll.kernel32
hid = ctypes.windll.hid


class GUID(ctypes.Structure):
    _fields_ = [
        ('Data1', wintypes.DWORD),
        ('Data2', wintypes.WORD),
        ('Data3', wintypes.WORD),
        ('Data4', ctypes.c_byte * 8),
    ]


GUID_HID = GUID(0x4D1E55B2, 0xF16F, 0x11CF, (0x88, 0xCB, 0x00, 0x11, 0x11, 0x00, 0x00, 0x30))

DIGCF_PRESENT = 0x02
DIGCF_DEVICEINTERFACE = 0x10

SPDRP_FRIENDLYNAME = 0x0000000C


class SP_DEVICE_INTERFACE_DATA(ctypes.Structure):
    _fields_ = [
        ('cbSize', wintypes.DWORD),
        ('InterfaceClassGuid', GUID),
        ('Flags', wintypes.DWORD),
        ('Reserved', ctypes.c_void_p),
    ]


class SP_DEVICE_INTERFACE_DETAIL_DATA_W(ctypes.Structure):
    _fields_ = [
        ('cbSize', wintypes.DWORD),
        ('DevicePath', wintypes.WCHAR * 260),
    ]


class SP_DEVINFO_DATA(ctypes.Structure):
    _fields_ = [
        ('cbSize', wintypes.DWORD),
        ('ClassGuid', GUID),
        ('DevInst', wintypes.DWORD),
        ('Reserved', ctypes.POINTER(ctypes.c_ulong)),
    ]


class HIDP_CAPS(ctypes.Structure):
    _fields_ = [
        ('Usage', wintypes.USHORT),
        ('UsagePage', wintypes.USHORT),
        ('InputReportByteLength', wintypes.USHORT),
        ('OutputReportByteLength', wintypes.USHORT),
        ('FeatureReportByteLength', wintypes.USHORT),
        ('Reserved', wintypes.USHORT * 17),
        ('NumberLinkCollectionNodes', wintypes.USHORT),
        ('NumberInputButtonCaps', wintypes.USHORT),
        ('NumberInputValueCaps', wintypes.USHORT),
        ('NumberInputDataIndices', wintypes.USHORT),
        ('NumberOutputButtonCaps', wintypes.USHORT),
        ('NumberOutputValueCaps', wintypes.USHORT),
        ('NumberOutputDataIndices', wintypes.USHORT),
        ('NumberFeatureButtonCaps', wintypes.USHORT),
        ('NumberFeatureValueCaps', wintypes.USHORT),
        ('NumberFeatureDataIndices', wintypes.USHORT),
    ]


setupapi.SetupDiGetClassDevsW.argtypes = [
    ctypes.POINTER(GUID), wintypes.LPCWSTR, wintypes.HWND, wintypes.DWORD,
]
setupapi.SetupDiGetClassDevsW.restype = wintypes.HANDLE

setupapi.SetupDiEnumDeviceInterfaces.argtypes = [
    wintypes.HANDLE, ctypes.c_void_p, ctypes.POINTER(GUID), wintypes.DWORD,
    ctypes.POINTER(SP_DEVICE_INTERFACE_DATA),
]
setupapi.SetupDiEnumDeviceInterfaces.restype = wintypes.BOOL

setupapi.SetupDiGetDeviceInterfaceDetailW.argtypes = [
    wintypes.HANDLE, ctypes.POINTER(SP_DEVICE_INTERFACE_DATA), ctypes.c_void_p,
    wintypes.DWORD, ctypes.POINTER(wintypes.DWORD), ctypes.POINTER(SP_DEVINFO_DATA),
]
setupapi.SetupDiGetDeviceInterfaceDetailW.restype = wintypes.BOOL

setupapi.SetupDiDestroyDeviceInfoList.argtypes = [wintypes.HANDLE]
setupapi.SetupDiDestroyDeviceInfoList.restype = wintypes.BOOL

setupapi.SetupDiGetDeviceRegistryPropertyW.argtypes = [
    wintypes.HANDLE, ctypes.POINTER(SP_DEVINFO_DATA), wintypes.DWORD,
    ctypes.POINTER(wintypes.DWORD), ctypes.c_void_p, wintypes.DWORD,
    ctypes.POINTER(wintypes.DWORD),
]
setupapi.SetupDiGetDeviceRegistryPropertyW.restype = wintypes.BOOL

hid.HidD_GetPreparsedData.argtypes = [wintypes.HANDLE, ctypes.POINTER(wintypes.LPVOID)]
hid.HidD_GetPreparsedData.restype = wintypes.BOOL
hid.HidD_FreePreparsedData.argtypes = [wintypes.LPVOID]
hid.HidD_FreePreparsedData.restype = wintypes.BOOL
hid.HidP_GetCaps.argtypes = [wintypes.LPVOID, ctypes.POINTER(HIDP_CAPS)]
hid.HidP_GetCaps.restype = ctypes.c_long
hid.HidD_GetProductString.argtypes = [wintypes.HANDLE, wintypes.LPVOID, wintypes.DWORD]
hid.HidD_GetProductString.restype = wintypes.BOOL

kernel32.CreateFileW.argtypes = [
    wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD, wintypes.LPVOID,
    wintypes.DWORD, wintypes.DWORD, wintypes.HANDLE,
]
kernel32.CreateFileW.restype = wintypes.HANDLE
kernel32.WriteFile.argtypes = [
    wintypes.HANDLE, wintypes.LPCVOID, wintypes.DWORD,
    ctypes.POINTER(wintypes.DWORD), wintypes.LPVOID,
]
kernel32.WriteFile.restype = wintypes.BOOL
kernel32.CloseHandle.argtypes = [wintypes.HANDLE]
kernel32.CloseHandle.restype = wintypes.BOOL

INVALID_HANDLE = wintypes.HANDLE(-1).value


@dataclass
class PixelBarDevice:
    path: str
    friendly_name: str
    usage_page: int
    usage: int
    input_report_length: int
    output_report_length: int


def _read_hid_string(handle: int, func) -> str:
    buf = ctypes.create_unicode_buffer(256)
    if func(handle, buf, ctypes.sizeof(buf)):
        return buf.value
    return ''


def _read_friendly_name(hdevinfo: int, devinfo: SP_DEVINFO_DATA) -> str:
    buf = ctypes.create_unicode_buffer(512)
    req = wintypes.DWORD()
    ok = setupapi.SetupDiGetDeviceRegistryPropertyW(
        hdevinfo, ctypes.byref(devinfo), SPDRP_FRIENDLYNAME, None,
        ctypes.byref(buf), ctypes.sizeof(buf), ctypes.byref(req),
    )
    if ok:
        return buf.value
    return ''


def _get_interface_path(hdevinfo: int, iface: SP_DEVICE_INTERFACE_DATA) -> tuple[str, SP_DEVINFO_DATA | None]:
    req_size = wintypes.DWORD()
    devinfo = SP_DEVINFO_DATA()
    devinfo.cbSize = ctypes.sizeof(devinfo)

    setupapi.SetupDiGetDeviceInterfaceDetailW(
        hdevinfo, ctypes.byref(iface), None, 0, ctypes.byref(req_size), ctypes.byref(devinfo),
    )
    if req_size.value == 0:
        return '', None

    buf = ctypes.create_string_buffer(req_size.value)
    ctypes.memset(buf, 0, req_size.value)
    detail_ptr = ctypes.cast(buf, ctypes.POINTER(SP_DEVICE_INTERFACE_DETAIL_DATA_W))
    detail_ptr.contents.cbSize = _DETAIL_CB_SIZE

    if not setupapi.SetupDiGetDeviceInterfaceDetailW(
        hdevinfo, ctypes.byref(iface), detail_ptr, req_size.value, None, ctypes.byref(devinfo),
    ):
        return '', None

    path = ctypes.wstring_at(ctypes.addressof(buf) + ctypes.sizeof(wintypes.DWORD))
    return path, devinfo


def _open_device(path: str) -> int:
    handle = kernel32.CreateFileW(
        path, GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        None, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, None,
    )
    if handle == INVALID_HANDLE:
        raise ctypes.WinError()
    return handle


def _probe_device(path: str, friendly_name: str) -> PixelBarDevice | None:
    try:
        handle = _open_device(path)
    except OSError:
        return None

    usage_page = 0
    usage = 0
    input_len = 0
    output_len = 0
    product = ''

    try:
        product = _read_hid_string(handle, hid.HidD_GetProductString)
        preparsed = wintypes.LPVOID()
        if hid.HidD_GetPreparsedData(handle, ctypes.byref(preparsed)):
            caps = HIDP_CAPS()
            if hid.HidP_GetCaps(preparsed, ctypes.byref(caps)) >= 0:
                usage_page = caps.UsagePage
                usage = caps.Usage
                input_len = caps.InputReportByteLength
                output_len = caps.OutputReportByteLength
            hid.HidD_FreePreparsedData(preparsed)
    finally:
        kernel32.CloseHandle(handle)

    name = friendly_name or product
    if product and product not in name:
        name = f'{name} ({product})' if name else product

    return PixelBarDevice(
        path=path,
        friendly_name=name,
        usage_page=usage_page,
        usage=usage,
        input_report_length=input_len,
        output_report_length=output_len,
    )


def find_pixelbar_devices(require_col02: bool = True) -> list[PixelBarDevice]:
    """枚举匹配的 PixelBar 屏幕 HID 接口。"""
    hdevinfo = setupapi.SetupDiGetClassDevsW(
        ctypes.byref(GUID_HID), None, None, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE,
    )
    if not hdevinfo or hdevinfo == INVALID_HANDLE:
        raise ctypes.WinError()

    matches: list[PixelBarDevice] = []
    try:
        index = 0
        while True:
            iface = SP_DEVICE_INTERFACE_DATA()
            iface.cbSize = ctypes.sizeof(iface)
            if not setupapi.SetupDiEnumDeviceInterfaces(
                hdevinfo, None, ctypes.byref(GUID_HID), index, ctypes.byref(iface),
            ):
                break
            index += 1

            path, devinfo = _get_interface_path(hdevinfo, iface)
            if not path:
                continue

            path_lower = path.lower()
            if f'vid_{PIXELBAR_VID:04x}' not in path_lower or f'pid_{PIXELBAR_PID:04x}' not in path_lower:
                continue
            if require_col02 and 'col02' not in path_lower:
                continue

            friendly = ''
            if devinfo is not None:
                friendly = _read_friendly_name(hdevinfo, devinfo)

            device = _probe_device(path, friendly)
            if device is None:
                continue
            if device.input_report_length != REPORT_LENGTH:
                continue
            matches.append(device)
    finally:
        setupapi.SetupDiDestroyDeviceInfoList(hdevinfo)

    return matches


def get_device_path() -> str:
    devices = find_pixelbar_devices()
    if not devices:
        raise RuntimeError(
            f'未找到 {FRIENDLY_NAME_HINT}（VID {PIXELBAR_VID:04X} / PID {PIXELBAR_PID:04X} / Col02 / {REPORT_LENGTH} 字节报告）'
        )
    return devices[0].path


def send_packet(pkt: bytes, path: str | None = None) -> int:
    device_path = path or get_device_path()
    handle = _open_device(device_path)
    try:
        written = wintypes.DWORD()
        ok = kernel32.WriteFile(handle, pkt, len(pkt), ctypes.byref(written), None)
        if not ok:
            raise ctypes.WinError()
        return written.value
    finally:
        kernel32.CloseHandle(handle)


def print_devices() -> None:
    devices = find_pixelbar_devices()
    if not devices:
        print(f'未找到匹配设备（{FRIENDLY_NAME_HINT}）')
        return
    for i, dev in enumerate(devices, 1):
        print(f'[{i}] {dev.friendly_name}')
        print(f'    path: {dev.path}')
        print(f'    UsagePage: 0x{dev.usage_page:04X}, Usage: 0x{dev.usage:04X}')
        print(f'    InputReport: {dev.input_report_length}, OutputReport: {dev.output_report_length}')


if __name__ == '__main__':
    print_devices()
