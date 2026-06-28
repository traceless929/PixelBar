"""Inspect EDIFIER TempoHub installation and running process."""

from __future__ import annotations

import sys
from pathlib import Path

import psutil

from tempohub_config import (
    DEFAULT_TEMPOHUB_DIR,
    DEFAULT_TEMPOHUB_EXE,
    HID_MODULE_HINTS,
    PROCESS_NAME_HINTS,
)


def find_running_processes() -> list[psutil.Process]:
    matches: list[psutil.Process] = []
    for proc in psutil.process_iter(["pid", "name", "exe"]):
        name = (proc.info.get("name") or "").lower()
        if any(hint in name for hint in PROCESS_NAME_HINTS):
            matches.append(proc)
    return matches


def inspect_installation(install_dir: Path) -> dict[str, object]:
    exe = Path(DEFAULT_TEMPOHUB_EXE)
    internal = install_dir / "_internal"
    hid_pyd = internal / "hid.cp39-win_amd64.pyd"

    return {
        "install_dir_exists": install_dir.is_dir(),
        "exe_exists": exe.is_file(),
        "exe_size_mb": round(exe.stat().st_size / (1024 * 1024), 2) if exe.is_file() else None,
        "hid_module_exists": hid_pyd.is_file(),
        "hid_module_size_kb": round(hid_pyd.stat().st_size / 1024, 1) if hid_pyd.is_file() else None,
        "internal_entries": len(list(internal.iterdir())) if internal.is_dir() else 0,
    }


def main() -> int:
    install_dir = Path(DEFAULT_TEMPOHUB_DIR)
    print("EDIFIER TempoHub")
    print(f"  install dir : {install_dir}")
    print(f"  executable  : {DEFAULT_TEMPOHUB_EXE}")

    info = inspect_installation(install_dir)
    print("\nInstallation")
    for key, value in info.items():
        print(f"  {key}: {value}")

    processes = find_running_processes()
    print("\nRunning processes")
    if not processes:
        print("  (none) — start TempoHub before running Frida capture")
    for proc in processes:
        print(f"  pid={proc.pid} name={proc.info.get('name')} exe={proc.info.get('exe')}")

    print("\nHID-related modules to hook")
    for hint in HID_MODULE_HINTS:
        print(f"  - {hint}")

    if not info["exe_exists"]:
        print("\nTempoHub executable not found. Update analysis/tempohub_config.py if installed elsewhere.")
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
