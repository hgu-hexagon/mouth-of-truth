from __future__ import annotations

from mouth_of_truth.contracts.verdict_kind import VerdictKind


LIE_THRESHOLD = 60.0


def get_verdict_from_score(score: float) -> VerdictKind:
    """Maps one fused score to the game-facing verdict enum."""
    if score >= LIE_THRESHOLD:
        return VerdictKind.FALSE

    return VerdictKind.TRUE
