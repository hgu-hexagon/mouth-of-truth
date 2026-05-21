"""Voice-emotion scoring rules for final judgment fusion."""

from __future__ import annotations

from collections import Counter
from typing import Any


BASE_TENSE_WEIGHT = 0.60
BASE_MEDIUM_WEIGHT = 0.25
BASE_STABLE_WEIGHT = 0.25
FINAL_BASE_WEIGHT = 0.65
FINAL_CHANGE_WEIGHT = 0.35


def _clamp(value: float, min_value: float, max_value: float) -> float:
    """Clamps one floating-point value to the provided range."""
    return max(min_value, min(value, max_value))


def calculate_voice_base_score(prob_dict: dict[str, float]) -> float:
    """Calculates one base suspicion score from voice-emotion probabilities."""
    stable_probability = prob_dict.get("neu", 0.0) + prob_dict.get("hap", 0.0)
    medium_probability = prob_dict.get("sad", 0.0) + prob_dict.get("exc", 0.0)
    tense_probability = prob_dict.get("ang", 0.0) + prob_dict.get("fru", 0.0)

    score = 100.0 * ((BASE_TENSE_WEIGHT * tense_probability) + (BASE_MEDIUM_WEIGHT * medium_probability) - (BASE_STABLE_WEIGHT * stable_probability))
    return _clamp(score, 0.0, 100.0)


def calculate_voice_change_score(current_probs: list[float], average_probs: list[float]) -> float:
    """Calculates one voice-emotion change score from probability deltas."""
    if not current_probs or not average_probs:
        return 0.0

    difference_sum = 0.0

    for current_probability, average_probability in zip(current_probs, average_probs):
        difference_sum += abs(current_probability - average_probability)

    return _clamp(difference_sum * 100.0, 0.0, 100.0)


def calculate_voice_suspicion_score(base_score: float, change_score: float) -> float:
    """Combines one base score and one change score into one final voice score."""
    return _clamp((FINAL_BASE_WEIGHT * base_score) + (FINAL_CHANGE_WEIGHT * change_score), 0.0, 100.0)


def get_voice_status_text(score: float) -> str:
    """Returns one human-readable voice status text from one score."""
    if score < 20.0:
        return "Calm"

    if score < 40.0:
        return "Stable"

    if score < 60.0:
        return "Slightly Nervous"

    if score < 80.0:
        return "Suspicious"

    return "Highly Unstable"


def summarize_voice_session(segment_results: list[dict[str, Any]]) -> dict[str, Any]:
    """Builds one voice-session summary from one sequence of segment results."""
    if not segment_results:
        return {
            "avg_score": 0.0,
            "avg_base": 0.0,
            "avg_change": 0.0,
            "dominant_label": "N/A",
            "status_text": "No data",
            "result_text": "No valid voice data",
        }

    segment_count = len(segment_results)
    average_score = sum(item["suspicion_score"] for item in segment_results) / segment_count
    average_base_score = sum(item["base_score"] for item in segment_results) / segment_count
    average_change_score = sum(item["change_score"] for item in segment_results) / segment_count
    label_counter = Counter(item["label"] for item in segment_results)
    dominant_label = label_counter.most_common(1)[0][0]
    status_text = get_voice_status_text(average_score)

    if average_score < 20.0:
        result_text = "Very calm voice response"
    elif average_score < 40.0:
        result_text = "Mostly stable voice response"
    elif average_score < 60.0:
        result_text = "Noticeable vocal tension"
    elif average_score < 80.0:
        result_text = "Voice-based suspicion increased"
    else:
        result_text = "Highly unstable vocal reaction"

    return {
        "avg_score": average_score,
        "avg_base": average_base_score,
        "avg_change": average_change_score,
        "dominant_label": dominant_label,
        "status_text": status_text,
        "result_text": result_text,
    }
