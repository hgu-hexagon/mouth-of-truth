from __future__ import annotations

from typing import Any

from mouth_of_truth.fusion.verdict_policy import get_verdict_from_score


FACE_WEIGHT = 0.5
VOICE_WEIGHT = 0.5


def clamp(value: float, min_value: float, max_value: float) -> float:
    """Clamps one floating-point value to the provided range."""
    return max(min_value, min(value, max_value))


def fuse_face_and_voice(face_result: dict[str, Any], voice_result: dict[str, Any]) -> dict[str, Any]:
    """Fuses face and voice summaries into one final verdict payload."""
    face_score = float(face_result.get("avg_score", 0.0))
    voice_score = float(voice_result.get("avg_score", 0.0))
    final_score = clamp(
        (face_score * FACE_WEIGHT) + (voice_score * VOICE_WEIGHT),
        0.0,
        100.0,
    )

    return {
        "face_score": face_score,
        "voice_score": voice_score,
        "final_score": final_score,
        "verdict": get_verdict_from_score(final_score),
    }
