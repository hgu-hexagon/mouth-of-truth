from __future__ import annotations

from mouth_of_truth.contracts.analysis_contracts import AnalysisResult
from mouth_of_truth.contracts.verdict_kind import VerdictKind
from mouth_of_truth.fusion.multimodal_fusion import fuse_face_and_voice


MIN_FACE_RECOGNITIONS_FOR_JUDGMENT = 5
MIN_VOICE_SEGMENTS_FOR_JUDGMENT = 1
INSUFFICIENT_FACE_DATA_REASON_CODE = "insufficient_face_data"
INSUFFICIENT_VOICE_DATA_REASON_CODE = "insufficient_voice_data"


def build_analysis_result(
    request_id: str,
    answer_transcript: str,
    face_result: dict,
    voice_result: dict,
    face_recognition_count: int,
    voice_segment_count: int,
) -> AnalysisResult:
    """Builds one final game-facing analysis result."""
    reason_codes: list[str] = []

    if face_recognition_count < MIN_FACE_RECOGNITIONS_FOR_JUDGMENT:
        reason_codes.append(INSUFFICIENT_FACE_DATA_REASON_CODE)

    if voice_segment_count < MIN_VOICE_SEGMENTS_FOR_JUDGMENT or not answer_transcript.strip():
        reason_codes.append(INSUFFICIENT_VOICE_DATA_REASON_CODE)

    if reason_codes:
        return AnalysisResult(
            request_id=request_id,
            verdict=VerdictKind.UNCERTAIN,
            answer_transcript=answer_transcript,
            reason_codes=reason_codes,
        )

    fused_result = fuse_face_and_voice(face_result, voice_result)
    return AnalysisResult(
        request_id=request_id,
        verdict=fused_result["verdict"],
        answer_transcript=answer_transcript,
        reason_codes=[],
    )
