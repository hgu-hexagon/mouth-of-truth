"""JSON bridge contracts shared by Unity and the Python analysis runner."""

from __future__ import annotations

import json
import os
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path

from mouth_of_truth.contracts.verdict_kind import VerdictKind


def build_utc_timestamp() -> str:
    """Builds one ISO 8601 UTC timestamp."""
    return datetime.now(timezone.utc).isoformat()


@dataclass(frozen=True)
class AnalysisRequest:
    """Represents one Unity-to-Python analysis request."""

    request_id: str
    question_id: str
    question_text: str
    answer_transcript: str
    answer_audio_file_path: str
    face_frames_directory_path: str
    face_frame_count: int
    voice_segment_count: int
    requested_at_utc: str


@dataclass(frozen=True)
class AnalysisResult:
    """Represents one Python-to-Unity analysis response."""

    request_id: str
    verdict: VerdictKind
    answer_transcript: str = ""
    reason_codes: list[str] = field(default_factory=list)
    completed_at_utc: str = field(default_factory=build_utc_timestamp)


def read_analysis_request(file_path: str | Path) -> AnalysisRequest:
    """Reads one analysis request from JSON."""
    file_path = Path(file_path).expanduser().resolve()
    payload = json.loads(file_path.read_text(encoding="utf-8"))
    answer_audio_relative_path = payload.get("AnswerAudioFilePath", "")
    face_frames_relative_path = payload.get("FaceFramesDirectoryPath", "")
    answer_audio_file_path = resolve_runtime_relative_path(file_path, answer_audio_relative_path)
    face_frames_directory_path = resolve_runtime_relative_path(file_path, face_frames_relative_path)

    return AnalysisRequest(request_id=payload["RequestID"], question_id=payload["QuestionID"], question_text=payload["QuestionText"], answer_transcript=payload.get("AnswerTranscript", ""), answer_audio_file_path=answer_audio_file_path, face_frames_directory_path=face_frames_directory_path, face_frame_count=int(payload.get("FaceFrameCount", 0)), voice_segment_count=int(payload.get("VoiceSegmentCount", 0)), requested_at_utc=payload["RequestedAtUtc"])


def write_analysis_result(file_path: str | Path, analysis_result: AnalysisResult) -> None:
    """Writes one analysis result JSON in the Unity bridge format."""
    file_path = Path(file_path)
    payload = {
        "RequestID": analysis_result.request_id,
        "Verdict": analysis_result.verdict.value,
        "AnswerTranscript": analysis_result.answer_transcript,
        "ReasonCodes": analysis_result.reason_codes,
        "CompletedAtUtc": analysis_result.completed_at_utc,
    }
    file_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


def resolve_runtime_relative_path(request_file_path: Path, raw_path: str) -> str:
    """Resolves one runtime-root-relative path from the request payload."""
    normalized_raw_path = raw_path.strip()

    if not normalized_raw_path:
        return ""

    candidate_path = Path(normalized_raw_path).expanduser()

    if candidate_path.is_absolute():
        return str(candidate_path.resolve())

    configured_runtime_root = os.environ.get("MOUTH_OF_TRUTH_RUNTIME_ROOT", "").strip()
    runtime_root_path = Path(configured_runtime_root).expanduser().resolve() if configured_runtime_root else request_file_path.parent
    return str((runtime_root_path / candidate_path).resolve())
