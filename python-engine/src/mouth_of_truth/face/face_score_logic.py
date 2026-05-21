"""Face-emotion scoring rules for final judgment fusion."""

from __future__ import annotations

from collections import Counter, deque
from typing import Any


BASE_TENSE_WEIGHT = 0.82
BASE_MEDIUM_WEIGHT = 0.52
BASE_STABLE_WEIGHT = 0.18
FINAL_BASE_WEIGHT = 0.60
FINAL_CHANGE_WEIGHT = 0.40


def _clamp(value: float, min_value: float, max_value: float) -> float:
    """Clamps one floating-point value to the provided range."""
    return max(min_value, min(value, max_value))


def get_average_distribution(history: deque[list[float]]) -> list[float]:
    """Calculates one average probability distribution from recognition history."""
    if not history:
        return []

    class_count = len(history[0])
    average_distribution = [0.0] * class_count

    for probabilities in history:
        for probability_index in range(class_count):
            average_distribution[probability_index] += probabilities[probability_index]

    for probability_index in range(class_count):
        average_distribution[probability_index] /= len(history)

    return average_distribution


def calculate_change_score(current_probs: list[float], average_probs: list[float]) -> float:
    """Calculates one face-emotion change score from probability deltas."""
    if not current_probs or not average_probs:
        return 0.0

    difference_sum = 0.0

    for current_probability, average_probability in zip(current_probs, average_probs):
        difference_sum += abs(current_probability - average_probability)

    return _clamp(difference_sum * 100.0, 0.0, 100.0)


def calculate_base_score(prob_dict: dict[str, float]) -> float:
    """Calculates one base suspicion score from face-emotion probabilities."""
    stable_probability = prob_dict.get("happiness", 0.0) + prob_dict.get("neutral", 0.0)
    medium_probability = prob_dict.get("sadness", 0.0) + prob_dict.get("surprise", 0.0)
    tense_probability = prob_dict.get("fear", 0.0) + prob_dict.get("disgust", 0.0) + prob_dict.get("anger", 0.0)

    score = 100.0 * ((BASE_TENSE_WEIGHT * tense_probability) + (BASE_MEDIUM_WEIGHT * medium_probability) + (BASE_STABLE_WEIGHT * stable_probability))
    return _clamp(score, 0.0, 100.0)


def calculate_suspicion_score(base_score: float, change_score: float) -> float:
    """Combines one base score and one change score into one final face score."""
    return _clamp((FINAL_BASE_WEIGHT * base_score) + (FINAL_CHANGE_WEIGHT * change_score), 0.0, 100.0)


def get_status_text(score: float) -> str:
    """Returns one human-readable face status text from one score."""
    if score < 20.0:
        return "Calm"

    if score < 40.0:
        return "Stable"

    if score < 60.0:
        return "Slightly Nervous"

    if score < 80.0:
        return "Suspicious"

    return "Highly Unstable"


def get_result_text(score: float) -> str:
    """Returns one human-readable face summary text from one score."""
    if score < 20.0:
        return "Very calm response"

    if score < 40.0:
        return "Mostly stable response"

    if score < 60.0:
        return "Noticeable tension"

    if score < 80.0:
        return "Suspicion increased"

    return "Highly unstable reaction"


def summarize_session(recognition_results: list[dict[str, Any]]) -> dict[str, Any]:
    """Builds one face-session summary from one sequence of recognition results."""
    if not recognition_results:
        return {
            "avg_score": 0.0,
            "avg_base": 0.0,
            "avg_change": 0.0,
            "dominant_label": "N/A",
            "status_text": "No data",
            "result_text": "No valid face data",
        }

    recognition_count = len(recognition_results)
    average_score = sum(item["suspicion_score"] for item in recognition_results) / recognition_count
    average_base_score = sum(item["base_score"] for item in recognition_results) / recognition_count
    change_score_sum = sum(item["change_score"] for item in recognition_results)
    average_change_score = change_score_sum / recognition_count
    label_counter = Counter(item["label"] for item in recognition_results)
    dominant_label = label_counter.most_common(1)[0][0]

    return {
        "avg_score": average_score,
        "avg_base": average_base_score,
        "avg_change": average_change_score,
        "dominant_label": dominant_label,
        "status_text": get_status_text(average_score),
        "result_text": get_result_text(average_score),
    }
