from __future__ import annotations

import contextlib
import json
import os
import sys
import traceback
from dataclasses import dataclass
from pathlib import Path
from typing import TextIO


def _ensure_package_root_on_sys_path() -> None:
    """Adds the python-engine src directory for direct script execution."""
    package_root_path = Path(__file__).resolve().parents[2]

    if str(package_root_path) not in sys.path:
        sys.path.insert(0, str(package_root_path))


_ensure_package_root_on_sys_path()

from mouth_of_truth.runners.bridge_analysis_runner import run_once


os.environ.setdefault("HF_HUB_DISABLE_PROGRESS_BARS", "1")


@dataclass(frozen=True)
class WorkerCommand:
    """Represents one stdin command sent by Unity."""

    command: str
    request_file_path: str
    result_file_path: str


def run_worker() -> int:
    """Runs a persistent analysis worker for Unity."""
    protocol_stdout = sys.stdout

    with contextlib.redirect_stdout(sys.stderr):
        _prewarm_models()

    _write_protocol_response(protocol_stdout, {"Status": "ready"})

    for raw_line in sys.stdin:
        command = _parse_worker_command(raw_line)

        if command.command == "shutdown":
            _write_protocol_response(protocol_stdout, {"Status": "shutdown"})
            return 0

        if command.command != "analyze":
            _write_protocol_response(
                protocol_stdout,
                {
                    "Status": "error",
                    "ErrorMessage": f"Unsupported worker command: {command.command}",
                },
            )
            continue

        try:
            with contextlib.redirect_stdout(sys.stderr):
                run_once(command.request_file_path, command.result_file_path)

            _write_protocol_response(protocol_stdout, {"Status": "done"})
        except Exception:
            _write_protocol_response(
                protocol_stdout,
                {
                    "Status": "error",
                    "ErrorMessage": traceback.format_exc(),
                },
            )

    return 0


def _prewarm_models() -> None:
    """Loads heavyweight models before the first answer reaches analysis."""
    try:
        from mouth_of_truth.face.infer_face import load_face_model

        load_face_model()
    except Exception:
        print(
            "Face model prewarm failed. The worker will still handle requests with fallback logic.\n"
            f"{traceback.format_exc()}",
            file=sys.stderr,
        )

    try:
        from mouth_of_truth.voice.infer_voice import load_voice_model

        load_voice_model()
    except Exception:
        print(
            "Voice model prewarm failed. The worker will still handle requests with fallback logic.\n"
            f"{traceback.format_exc()}",
            file=sys.stderr,
        )


def _parse_worker_command(raw_line: str) -> WorkerCommand:
    """Parses one JSON-line worker command."""
    payload = json.loads(raw_line)
    return WorkerCommand(
        command=str(payload.get("Command", "")).strip().lower(),
        request_file_path=str(payload.get("RequestFilePath", "")).strip(),
        result_file_path=str(payload.get("ResultFilePath", "")).strip(),
    )


def _write_protocol_response(protocol_stdout: TextIO, payload: dict[str, str]) -> None:
    """Writes one JSON-line response to Unity."""
    print(json.dumps(payload, ensure_ascii=True), file=protocol_stdout, flush=True)


def main() -> int:
    """Runs the bridge analysis worker."""
    return run_worker()


if __name__ == "__main__":
    raise SystemExit(main())
