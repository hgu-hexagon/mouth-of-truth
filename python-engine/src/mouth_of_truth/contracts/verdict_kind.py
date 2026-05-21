"""Game-facing verdict enum values."""

from __future__ import annotations

from enum import Enum


class VerdictKind(str, Enum):
    """Represents the game-facing verdict labels."""

    TRUE = "TRUE"
    FALSE = "FALSE"
    UNCERTAIN = "UNCERTAIN"
