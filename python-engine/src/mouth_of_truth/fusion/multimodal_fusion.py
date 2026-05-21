"""Multimodal face-and-voice score fusion helpers."""

from __future__ import annotations

from typing import Any

from mouth_of_truth.fusion.verdict_policy import get_multimodal_verdict_from_score


FACE_WEIGHT = 0.80
VOICE_WEIGHT = 0.20
EmotionSummary = dict[str, Any]
FusedVerdictPayload = dict[str, Any]


def _clamp(value: float, min_value: float, max_value: float) -> float:
    """Clamps one floating-point value to the provided range."""
    return max(min_value, min(value, max_value))


def fuse_face_and_voice(face_result: EmotionSummary, voice_result: EmotionSummary) -> FusedVerdictPayload:
    """Fuses face and voice summaries into one final verdict payload."""
    face_score = float(face_result.get("avg_score", 0.0))
    voice_score = float(voice_result.get("avg_score", 0.0))
    final_score = _clamp((face_score * FACE_WEIGHT) + (voice_score * VOICE_WEIGHT), 0.0, 100.0)
    verdict = get_multimodal_verdict_from_score(final_score)

    return {
        "face_score": face_score,
        "voice_score": voice_score,
        "final_score": final_score,
        "verdict": verdict,
        "reason_codes": [],
    }
