"""Quick Frida attach smoke test for TempoHub."""

import sys
from pathlib import Path

import frida
import psutil

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "analysis"))

from tempohub_config import PROCESS_NAME_HINTS  # noqa: E402


def main() -> int:
    pid = next(
        p.info["pid"]
        for p in psutil.process_iter(["pid", "name"])
        if any(h in (p.info.get("name") or "").lower() for h in PROCESS_NAME_HINTS)
    )
    print(f"attaching pid={pid}")

    device = frida.get_local_device()
    session = device.attach(pid)
    script = session.create_script(
        """
        var hid = Process.findModuleByName('hid.cp39-win_amd64.pyd');
        send('hid.pyd=' + (hid ? hid.base : 'not loaded'));
        var k32 = Process.findModuleByName('kernel32.dll');
        send('WriteFile=' + k32.findExportByName('WriteFile'));
        """
    )
    script.on("message", lambda message, _data: print(message.get("payload", message)))
    script.load()
    session.detach()
    print("attach ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
