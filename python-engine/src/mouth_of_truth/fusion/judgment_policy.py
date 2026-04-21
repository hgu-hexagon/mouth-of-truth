from __future__ import annotations

from mouth_of_truth.contracts.analysis_contracts import AnalysisResult
from mouth_of_truth.contracts.verdict_kind import VerdictKind
from mouth_of_truth.fusion.multimodal_fusion import fuse_face_and_voice
from mouth_of_truth.fusion.verdict_policy import get_verdict_from_score


MIN_FACE_RECOGNITIONS_FOR_JUDGMENT = 4
MIN_VOICE_SEGMENTS_FOR_JUDGMENT = 1
INSUFFICIENT_FACE_DATA_REASON_CODE = "insufficient_face_data"
INSUFFICIENT_VOICE_DATA_REASON_CODE = "insufficient_voice_data"
VOICE_ONLY_JUDGMENT_REASON_CODE = "voice_only_judgment"


def build_analysis_result(
    request_id: str,
    answer_transcript: str,
    face_result: dict,
    voice_result: dict,
    face_recognition_count: int,
    voice_segment_count: int,
) -> AnalysisResult:
    """Builds one final game-facing analysis result."""
    has_face_signal = face_recognition_count >= MIN_FACE_RECOGNITIONS_FOR_JUDGMENT
    has_voice_signal = voice_segment_count >= MIN_VOICE_SEGMENTS_FOR_JUDGMENT
    reason_codes = build_missing_signal_reason_codes(has_face_signal, has_voice_signal)

    if has_face_signal and has_voice_signal:
        fused_result = fuse_face_and_voice(face_result, voice_result)
        return AnalysisResult(
            request_id=request_id,
            verdict=fused_result["verdict"],
            answer_transcript=answer_transcript,
            reason_codes=[],
        )

    if has_voice_signal:
        return AnalysisResult(
            request_id=request_id,
            verdict=get_verdict_from_score(float(voice_result.get("avg_score", 0.0))),
            answer_transcript=answer_transcript,
            reason_codes=append_reason_code(
                reason_codes,
                VOICE_ONLY_JUDGMENT_REASON_CODE,
            ),
        )

    return AnalysisResult(
        request_id=request_id,
        verdict=VerdictKind.UNCERTAIN,
        answer_transcript=answer_transcript,
        reason_codes=reason_codes,
    )


def build_missing_signal_reason_codes(
    has_face_signal: bool,
    has_voice_signal: bool,
) -> list[str]:
    """Builds one stable reason-code list for missing evidence signals."""
    reason_codes: list[str] = []

    if has_face_signal is False:
        reason_codes.append(INSUFFICIENT_FACE_DATA_REASON_CODE)

    if has_voice_signal is False:
        reason_codes.append(INSUFFICIENT_VOICE_DATA_REASON_CODE)

    return reason_codes


def append_reason_code(reason_codes: list[str], reason_code: str) -> list[str]:
    """Returns one reason-code list with the requested code appended once."""
    if reason_code in reason_codes:
        return reason_codes

    return [*reason_codes, reason_code]
