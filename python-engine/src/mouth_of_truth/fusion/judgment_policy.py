"""Final judgment policy for multimodal face and voice evidence."""

from __future__ import annotations

from typing import Any

from mouth_of_truth.contracts.analysis_contracts import AnalysisResult
from mouth_of_truth.contracts.verdict_kind import VerdictKind
from mouth_of_truth.fusion.multimodal_fusion import fuse_face_and_voice


MIN_FACE_RECOGNITIONS_FOR_JUDGMENT = 1
MIN_VOICE_SEGMENTS_FOR_JUDGMENT = 1
INSUFFICIENT_FACE_DATA_REASON_CODE = "insufficient_face_data"
INSUFFICIENT_VOICE_DATA_REASON_CODE = "insufficient_voice_data"
AnalysisSummary = dict[str, Any]
FusedVerdictPayload = dict[str, Any]


def build_analysis_result(request_id: str, answer_transcript: str, face_result: AnalysisSummary, voice_result: AnalysisSummary, face_recognition_count: int, voice_segment_count: int) -> AnalysisResult:
    """Builds one final game-facing analysis result."""
    has_face_signal = face_recognition_count >= MIN_FACE_RECOGNITIONS_FOR_JUDGMENT
    has_voice_signal = voice_segment_count >= MIN_VOICE_SEGMENTS_FOR_JUDGMENT
    has_face_evidence = has_face_signal and _has_face_summary_signal(face_result)
    has_voice_evidence = has_voice_signal and _has_voice_summary_signal(voice_result)
    reason_codes = _build_missing_signal_reason_codes(has_face_evidence, has_voice_evidence)

    if has_face_evidence and has_voice_evidence:
        fused_result: FusedVerdictPayload = fuse_face_and_voice(face_result, voice_result)
        return AnalysisResult(request_id=request_id, verdict=fused_result["verdict"], answer_transcript=answer_transcript, reason_codes=fused_result["reason_codes"])

    return AnalysisResult(request_id=request_id, verdict=VerdictKind.UNCERTAIN, answer_transcript=answer_transcript, reason_codes=reason_codes)


def _has_face_summary_signal(face_result: AnalysisSummary) -> bool:
    """Returns whether face analysis produced one usable session summary."""
    return str(face_result.get("dominant_label", "N/A")).strip().upper() != "N/A"


def _has_voice_summary_signal(voice_result: AnalysisSummary) -> bool:
    """Returns whether voice analysis produced one usable session summary."""
    return str(voice_result.get("dominant_label", "N/A")).strip().upper() != "N/A"


def _build_missing_signal_reason_codes(has_face_evidence: bool, has_voice_evidence: bool) -> list[str]:
    """Builds one stable reason-code list for missing evidence signals."""
    reason_codes: list[str] = []

    if has_face_evidence is False:
        reason_codes.append(INSUFFICIENT_FACE_DATA_REASON_CODE)

    if has_voice_evidence is False:
        reason_codes.append(INSUFFICIENT_VOICE_DATA_REASON_CODE)

    return reason_codes
