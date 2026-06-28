# 官方 PC 端软件安装路径（可按本机修改，或通过环境变量 TEMPOHUB_DIR 覆盖）
import os

_DEFAULT_DIR = os.environ.get(
    "TEMPOHUB_DIR",
    r"C:\Program Files (x86)\EDIFIER TempoHub",
)

# 若存在 analysis/tempohub_config_local.py，可覆盖下列默认值（该文件已 gitignore）
try:
    from tempohub_config_local import *  # noqa: F403
except ImportError:
    DEFAULT_TEMPOHUB_DIR = _DEFAULT_DIR
    DEFAULT_TEMPOHUB_EXE = os.path.join(_DEFAULT_DIR, "EDIFIER TempoHub.exe")

# 运行时进程名匹配（不区分大小写，子串匹配）
PROCESS_NAME_HINTS = (
    "tempohub",
    "edifier tempohub",
)

# HID 通信相关模块
HID_MODULE_HINTS = (
    "hid.cp39-win_amd64.pyd",
    "hid.dll",
    "hidapi.dll",
)
