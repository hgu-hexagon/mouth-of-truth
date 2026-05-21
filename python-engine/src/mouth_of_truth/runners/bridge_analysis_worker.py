"""Persistent bridge analysis worker used by Unity release builds."""

from __future__ import annotations

import contextlib
import json
import os
import sys
import traceback
from collections.abc import Callable
from concurrent.futures import ThreadPoolExecutor
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
            _write_protocol_response(protocol_stdout, {"Status": "error", "ErrorMessage": f"Unsupported worker command: {command.command}"})
            continue

        try:
            with contextlib.redirect_stdout(sys.stderr):
                run_once(command.request_file_path, command.result_file_path)

            _write_protocol_response(protocol_stdout, {"Status": "done"})
        except Exception:
            _write_protocol_response(protocol_stdout, {"Status": "error", "ErrorMessage": traceback.format_exc()})

    return 0


def _prewarm_models() -> None:
    """Loads heavyweight models before the first answer reaches analysis."""
    with ThreadPoolExecutor(max_workers=2) as executor:
        futures = [executor.submit(_prewarm_face_model)]

        if _should_prewarm_trained_voice_model():
            futures.append(executor.submit(_prewarm_voice_model))

        for future in futures:
            future.result()


def _prewarm_face_model() -> None:
    """Loads the face model cache for the persistent worker."""
    from mouth_of_truth.face.infer_face import load_face_model

    _prewarm_model(load_face_model, "Face model prewarm failed. The worker will still handle requests with fallback logic.")


def _prewarm_voice_model() -> None:
    """Loads the voice model cache for the persistent worker."""
    from mouth_of_truth.voice.infer_voice import load_voice_model

    _prewarm_model(load_voice_model, "Voice model prewarm failed. The worker will still handle requests with fallback logic.")


def _should_prewarm_trained_voice_model() -> bool:
    """Returns whether the optional trained voice model should be prewarmed."""
    from mouth_of_truth.voice.voice_emotion_pipeline import should_use_trained_voice_model

    return should_use_trained_voice_model()


def _prewarm_model(load_model: Callable[[], object], failure_message: str) -> None:
    """Runs one model warm-up step without failing the worker."""
    try:
        load_model()
    except Exception:
        print(f"{failure_message}\n{traceback.format_exc()}", file=sys.stderr)


def _parse_worker_command(raw_line: str) -> WorkerCommand:
    """Parses one JSON-line worker command."""
    payload = json.loads(raw_line)
    return WorkerCommand(command=str(payload.get("Command", "")).strip().lower(), request_file_path=str(payload.get("RequestFilePath", "")).strip(), result_file_path=str(payload.get("ResultFilePath", "")).strip())


def _write_protocol_response(protocol_stdout: TextIO, payload: dict[str, str]) -> None:
    """Writes one JSON-line response to Unity."""
    print(json.dumps(payload, ensure_ascii=True), file=protocol_stdout, flush=True)


def main() -> int:
    """Runs the bridge analysis worker."""
    return run_worker()


if __name__ == "__main__":
    raise SystemExit(main())
