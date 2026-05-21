"""Score-to-verdict mapping rules."""

from __future__ import annotations

from mouth_of_truth.contracts.verdict_kind import VerdictKind


MULTIMODAL_FALSE_PIVOT_SCORE = 33.0


def get_multimodal_verdict_from_score(score: float) -> VerdictKind:
    """Maps one fused multimodal score to the game-facing verdict enum."""
    return VerdictKind.TRUE if score < MULTIMODAL_FALSE_PIVOT_SCORE else VerdictKind.FALSE
