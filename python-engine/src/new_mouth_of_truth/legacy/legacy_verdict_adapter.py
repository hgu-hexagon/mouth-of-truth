from __future__ import annotations

from new_mouth_of_truth.contracts.verdict_kind import VerdictKind


def map_legacy_verdict_to_game_verdict(legacy_verdict: str) -> VerdictKind:
    """Maps the old engine verdict names to the new game-facing names."""
    normalized_verdict = legacy_verdict.strip().lower()

    if normalized_verdict == "truth":
        return VerdictKind.TRUE

    if normalized_verdict == "lie":
        return VerdictKind.FALSE

    return VerdictKind.UNCERTAIN
