"""Deterministic analysis runner for presentation and regression validation."""

from __future__ import annotations

from hashlib import sha256
from pathlib import Path

from mouth_of_truth.contracts.analysis_contracts import (
    AnalysisResult,
    read_analysis_request,
    write_analysis_result,
)
from mouth_of_truth.contracts.verdict_kind import VerdictKind


MINIMUM_FACE_RECOGNITION_COUNT = 4
MINIMUM_VOICE_SEGMENT_COUNT = 1
INSUFFICIENT_FACE_DATA_REASON_CODE = "insufficient_face_data"
INSUFFICIENT_VOICE_DATA_REASON_CODE = "insufficient_voice_data"


def build_deterministic_analysis_result(request_file_path: str | Path) -> AnalysisResult:
    """Builds one deterministic validation result from a Unity bridge request."""
    analysis_request = read_analysis_request(request_file_path)

    has_face_signal = analysis_request.face_frame_count >= MINIMUM_FACE_RECOGNITION_COUNT
    has_voice_signal = analysis_request.voice_segment_count >= MINIMUM_VOICE_SEGMENT_COUNT
    reason_codes = _build_missing_signal_reason_codes(has_face_signal, has_voice_signal)

    if has_face_signal is False or has_voice_signal is False:
        return AnalysisResult(
            request_id=analysis_request.request_id,
            verdict=VerdictKind.UNCERTAIN,
            answer_transcript=analysis_request.answer_transcript,
            reason_codes=reason_codes,
        )

    stable_hash = sha256(f"{analysis_request.question_id}|{analysis_request.answer_transcript}".encode("utf-8")).hexdigest()
    verdict = VerdictKind.TRUE if int(stable_hash[-1], 16) % 2 == 0 else VerdictKind.FALSE

    return AnalysisResult(
        request_id=analysis_request.request_id,
        verdict=verdict,
        answer_transcript=analysis_request.answer_transcript,
        reason_codes=reason_codes,
    )


def run_once(request_file_path: str | Path, result_file_path: str | Path) -> None:
    """Reads one request JSON and writes one result JSON."""
    analysis_result = build_deterministic_analysis_result(request_file_path)
    write_analysis_result(result_file_path, analysis_result)


def _build_missing_signal_reason_codes(has_face_signal: bool, has_voice_signal: bool) -> list[str]:
    """Builds one stable reason-code list for missing deterministic signals."""
    reason_codes: list[str] = []

    if has_face_signal is False:
        reason_codes.append(INSUFFICIENT_FACE_DATA_REASON_CODE)

    if has_voice_signal is False:
        reason_codes.append(INSUFFICIENT_VOICE_DATA_REASON_CODE)

    return reason_codes
