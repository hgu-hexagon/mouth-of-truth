from __future__ import annotations

from hashlib import sha256
from pathlib import Path

from mouth_of_truth.contracts.analysis_contracts import (
    AnalysisResult,
    read_analysis_request,
    write_analysis_result,
)
from mouth_of_truth.contracts.verdict_kind import VerdictKind


MINIMUM_FACE_RECOGNITION_COUNT = 5
MINIMUM_VOICE_SEGMENT_COUNT = 1


def build_deterministic_analysis_result(
    request_file_path: str | Path,
) -> AnalysisResult:
    """Builds one deterministic placeholder result from a Unity bridge request."""
    analysis_request = read_analysis_request(request_file_path)

    reason_codes: list[str] = []

    if analysis_request.face_recognition_count < MINIMUM_FACE_RECOGNITION_COUNT:
        reason_codes.append("insufficient_face_data")

    if analysis_request.voice_segment_count < MINIMUM_VOICE_SEGMENT_COUNT:
        reason_codes.append("insufficient_voice_data")

    if reason_codes:
        return AnalysisResult(
            request_id=analysis_request.request_id,
            verdict=VerdictKind.UNCERTAIN,
            answer_transcript=analysis_request.answer_transcript,
            reason_codes=reason_codes,
        )

    stable_hash = sha256(
        f"{analysis_request.question_id}|{analysis_request.answer_transcript}".encode(
            "utf-8"
        )
    ).hexdigest()
    verdict = VerdictKind.TRUE if int(stable_hash[-1], 16) % 2 == 0 else VerdictKind.FALSE

    return AnalysisResult(
        request_id=analysis_request.request_id,
        verdict=verdict,
        answer_transcript=analysis_request.answer_transcript,
    )


def run_once(request_file_path: str | Path, result_file_path: str | Path) -> None:
    """Reads one request JSON and writes one result JSON."""
    analysis_result = build_deterministic_analysis_result(request_file_path)
    write_analysis_result(result_file_path, analysis_result)
