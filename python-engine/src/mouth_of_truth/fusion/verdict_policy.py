from __future__ import annotations

from mouth_of_truth.contracts.verdict_kind import VerdictKind


MULTIMODAL_TRUE_MAX_SCORE = 40.0
MULTIMODAL_FALSE_MIN_SCORE = 60.0
FACE_ONLY_TRUE_MAX_SCORE = 35.0
FACE_ONLY_FALSE_MIN_SCORE = 65.0
DISCORDANT_SIGNAL_SCORE_GAP = 35.0
DISCORDANT_SIGNAL_MIN_FINAL_SCORE = 40.0
DISCORDANT_SIGNAL_MAX_FINAL_SCORE = 60.0


def _get_verdict_from_score(
    score: float,
    true_max_score: float,
    false_min_score: float,
) -> VerdictKind:
    """Maps one suspicion score to the game-facing verdict enum."""
    if score < true_max_score:
        return VerdictKind.TRUE

    if score >= false_min_score:
        return VerdictKind.FALSE

    return VerdictKind.UNCERTAIN


def get_multimodal_verdict_from_score(score: float) -> VerdictKind:
    """Maps one fused multimodal score to the game-facing verdict enum."""
    return _get_verdict_from_score(
        score,
        true_max_score=MULTIMODAL_TRUE_MAX_SCORE,
        false_min_score=MULTIMODAL_FALSE_MIN_SCORE,
    )


def get_face_only_verdict_from_score(score: float) -> VerdictKind:
    """Maps one face-only score to one conservative verdict enum."""
    return _get_verdict_from_score(
        score,
        true_max_score=FACE_ONLY_TRUE_MAX_SCORE,
        false_min_score=FACE_ONLY_FALSE_MIN_SCORE,
    )


def should_mark_discordant_multimodal_signal(
    face_score: float,
    voice_score: float,
    final_score: float,
) -> bool:
    """Returns whether face and voice disagree too strongly for a safe verdict."""
    return (
        abs(face_score - voice_score) >= DISCORDANT_SIGNAL_SCORE_GAP
        and DISCORDANT_SIGNAL_MIN_FINAL_SCORE
        <= final_score
        <= DISCORDANT_SIGNAL_MAX_FINAL_SCORE
    )
