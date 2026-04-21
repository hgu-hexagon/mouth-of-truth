from __future__ import annotations

from typing import Any

from mouth_of_truth.contracts.verdict_kind import VerdictKind
from mouth_of_truth.fusion.verdict_policy import (
    get_multimodal_verdict_from_score,
    should_mark_discordant_multimodal_signal,
)


FACE_WEIGHT = 0.75
VOICE_WEIGHT = 0.25
AMBIGUOUS_MULTIMODAL_SIGNAL_REASON_CODE = "ambiguous_multimodal_signal"
DISCORDANT_MULTIMODAL_SIGNAL_REASON_CODE = "discordant_multimodal_signal"
EmotionSummary = dict[str, Any]
FusedVerdictPayload = dict[str, Any]


def _clamp(value: float, min_value: float, max_value: float) -> float:
    """Clamps one floating-point value to the provided range."""
    return max(min_value, min(value, max_value))


def fuse_face_and_voice(
    face_result: EmotionSummary,
    voice_result: EmotionSummary,
) -> FusedVerdictPayload:
    """Fuses face and voice summaries into one final verdict payload."""
    face_score = float(face_result.get("avg_score", 0.0))
    voice_score = float(voice_result.get("avg_score", 0.0))
    final_score = _clamp((face_score * FACE_WEIGHT) + (voice_score * VOICE_WEIGHT), 0.0, 100.0)
    reason_codes: list[str] = []

    if should_mark_discordant_multimodal_signal(face_score, voice_score, final_score):
        verdict = VerdictKind.UNCERTAIN
        reason_codes.append(DISCORDANT_MULTIMODAL_SIGNAL_REASON_CODE)
    else:
        verdict = get_multimodal_verdict_from_score(final_score)

        if verdict == VerdictKind.UNCERTAIN:
            reason_codes.append(AMBIGUOUS_MULTIMODAL_SIGNAL_REASON_CODE)

    return {
        "face_score": face_score,
        "voice_score": voice_score,
        "final_score": final_score,
        "verdict": verdict,
        "reason_codes": reason_codes,
    }
