"""Attach Frida to EDIFIER TempoHub and capture HID traffic."""

from __future__ import annotations

import argparse
import sys
import time
from datetime import datetime
from pathlib import Path

import frida
import psutil

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "analysis"))

from tempohub_config import DEFAULT_TEMPOHUB_EXE, PROCESS_NAME_HINTS  # noqa: E402


def find_tempohub_pid() -> int | None:
    for proc in psutil.process_iter(["pid", "name"]):
        name = (proc.info.get("name") or "").lower()
        if any(hint in name for hint in PROCESS_NAME_HINTS):
            return proc.info["pid"]
    return None


def load_js(filter_mode: str) -> str:
    template = (Path(__file__).with_name("hid_capture.js")).read_text(encoding="utf-8")
    return template.replace("{{FILTER_MODE}}", filter_mode)


def main() -> int:
    parser = argparse.ArgumentParser(description="Capture HID packets from EDIFIER TempoHub")
    parser.add_argument("--spawn", action="store_true", help="Launch TempoHub and attach on start")
    parser.add_argument("--pid", type=int, help="Attach to a specific PID")
    parser.add_argument(
        "--filter",
        choices=("packet", "all"),
        default="packet",
        help="packet=only 2E AA EC 64-byte frames; all=log every WriteFile/ReadFile",
    )
    parser.add_argument(
        "--log",
        type=Path,
        default=ROOT / "capture" / "usb_log.txt",
        help="Output log file path",
    )
    args = parser.parse_args()

    args.log.parent.mkdir(parents=True, exist_ok=True)
    device = frida.get_local_device()
    session: frida.core.Session
    spawn_pid: int | None = None

    if args.spawn:
        if not Path(DEFAULT_TEMPOHUB_EXE).is_file():
            print(f"TempoHub not found: {DEFAULT_TEMPOHUB_EXE}")
            print("Update analysis/tempohub_config.py with your install path.")
            return 1
        spawn_pid = device.spawn([DEFAULT_TEMPOHUB_EXE])
        session = device.attach(spawn_pid)
    elif args.pid:
        session = device.attach(args.pid)
    else:
        pid = find_tempohub_pid()
        if not pid:
            print("TempoHub is not running.")
            print(f"Start: {DEFAULT_TEMPOHUB_EXE}")
            print("Or run: python hooks/capture_tempohub.py --spawn")
            return 1
        session = device.attach(pid)
        spawn_pid = pid

    log_file = args.log.open("a", encoding="utf-8")
    log_file.write(f"\n--- capture started {datetime.now().isoformat(timespec='seconds')} filter={args.filter} ---\n")
    log_file.flush()

    def on_message(message, _data):
        if message["type"] != "send":
            line = str(message)
        else:
            line = message["payload"]
        print(line, flush=True)
        log_file.write(line + "\n")
        log_file.flush()

    script = session.create_script(load_js(args.filter))
    script.on("message", on_message)
    script.load()

    if args.spawn:
        device.resume(spawn_pid)
        print(f"Spawned TempoHub pid={spawn_pid}", flush=True)
    else:
        print(f"Attached pid={spawn_pid or args.pid or find_tempohub_pid()}", flush=True)

    print(f"Logging to {args.log}", flush=True)
    print("In TempoHub: change pixel text / RGB / pattern, then Ctrl+C to stop.", flush=True)

    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        pass
    finally:
        session.detach()
        log_file.close()
        print("Stopped.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
