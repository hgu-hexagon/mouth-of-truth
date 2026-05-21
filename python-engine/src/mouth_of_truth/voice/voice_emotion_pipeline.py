"""Voice-emotion analysis pipeline for recorded answer audio."""

from __future__ import annotations

import math
import os
from collections import deque
from typing import Any

import torch

from mouth_of_truth.audio_signal import calculate_window_rms
from mouth_of_truth.voice.infer_voice import TARGET_SAMPLE_RATE, VOICE_LABELS, load_audio, load_voice_model, probs_to_dict
from mouth_of_truth.voice.voice_score_logic import calculate_voice_base_score, calculate_voice_change_score, calculate_voice_suspicion_score, get_voice_status_text, summarize_voice_session


SEGMENT_SECONDS = 2.0
SEGMENT_STRIDE_SECONDS = 1.0
MAX_ANALYSIS_SEGMENT_COUNT = 1
VOICE_HISTORY_SIZE = 10
FAST_WINDOW_SECONDS = 0.20
FAST_STRIDE_SECONDS = 0.10
FAST_SPEECH_EVIDENCE_RMS_THRESHOLD = 0.0145
FAST_SPEECH_EVIDENCE_PEAK_RMS_THRESHOLD = 0.0200
MINIMUM_FAST_SPEECH_EVIDENCE_WINDOW_COUNT = 4
TRAINED_VOICE_MODEL_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_USE_TRAINED_VOICE_MODEL"


def _clamp(value: float, min_value: float, max_value: float) -> float:
    """Clamps one floating-point value to the provided range."""
    return max(min_value, min(value, max_value))


def run_voice_emotion_pipeline(audio_path: str) -> dict[str, Any]:
    """Runs one voice-emotion analysis pipeline on one recorded answer file."""
    waveform = load_audio(audio_path)

    if has_sustained_voice_evidence(waveform, TARGET_SAMPLE_RATE) is False:
        return build_empty_voice_analysis()

    if should_use_trained_voice_model():
        return run_trained_voice_emotion_pipeline(audio_path, waveform)

    segment_result = build_fast_voice_segment_result(waveform, TARGET_SAMPLE_RATE)
    return {
        "audio_path": audio_path,
        "segment_count": 1,
        "segments": [segment_result],
        "summary": summarize_voice_session([segment_result]),
    }


def should_use_trained_voice_model() -> bool:
    """Returns whether to use the heavyweight trained voice-emotion model."""
    configured_value = os.environ.get(TRAINED_VOICE_MODEL_ENVIRONMENT_VARIABLE_NAME, "")
    return configured_value.strip().lower() in {"1", "true", "yes", "on"}


def run_trained_voice_emotion_pipeline(audio_path: str, waveform: list[float]) -> dict[str, Any]:
    """Runs the slower trained voice-emotion model for offline validation."""
    feature_extractor, model = load_voice_model()
    segments = [segment_waveform for segment_waveform in split_audio_into_segments(waveform, TARGET_SAMPLE_RATE) if has_speech_signal(segment_waveform, TARGET_SAMPLE_RATE)]
    segments = select_representative_segments(segments)
    history: deque[list[float]] = deque(maxlen=VOICE_HISTORY_SIZE)
    segment_results: list[dict[str, Any]] = []

    analyzed_segment_index = 0

    for segment_waveform in segments:
        prediction = predict_voice_segment(feature_extractor, model, segment_waveform)
        probabilities_data = prediction["probs"]
        history.append(probabilities_data)
        average_distribution = build_average_distribution(history)
        change_score = calculate_voice_change_score(probabilities_data, average_distribution)
        base_score = calculate_voice_base_score(prediction["prob_dict"])
        suspicion_score = calculate_voice_suspicion_score(base_score, change_score)

        segment_results.append({"segment_index": analyzed_segment_index, "label": prediction["label"], "confidence": prediction["confidence"], "change_score": change_score, "base_score": base_score, "suspicion_score": suspicion_score, "status_text": get_voice_status_text(suspicion_score), "prob_dict": prediction["prob_dict"]})
        analyzed_segment_index += 1

    return {
        "audio_path": audio_path,
        "segment_count": len(segment_results),
        "segments": segment_results,
        "summary": summarize_voice_session(segment_results),
    }


def build_fast_voice_segment_result(waveform: list[float], sample_rate: int) -> dict[str, Any]:
    """Builds one quick voice-instability summary from waveform dynamics."""
    rms_values = calculate_rms_windows(waveform, sample_rate)
    speech_rms_values = [rms_value for rms_value in rms_values if rms_value >= FAST_SPEECH_EVIDENCE_RMS_THRESHOLD]

    if not speech_rms_values:
        probability_dict = build_fast_voice_probability_dict(0.0)
        return {
            "segment_index": 0,
            "label": "neu",
            "confidence": probability_dict["neu"],
            "change_score": 0.0,
            "base_score": 0.0,
            "suspicion_score": 0.0,
            "status_text": get_voice_status_text(0.0),
            "prob_dict": probability_dict,
            "probs": [probability_dict[label] for label in VOICE_LABELS],
        }

    average_rms = sum(speech_rms_values) / len(speech_rms_values)
    max_rms = max(speech_rms_values)
    energy_variance = sum((rms_value - average_rms) ** 2 for rms_value in speech_rms_values)
    energy_deviation = math.sqrt(energy_variance / len(speech_rms_values))
    speech_density = len(speech_rms_values) / max(1, len(rms_values))
    change_score = _clamp((energy_deviation / max(average_rms, 0.0001)) * 32.0, 0.0, 34.0)
    gap_score = _clamp((1.0 - speech_density) * 18.0, 0.0, 18.0)
    spike_score = _clamp(((max_rms / max(average_rms, 0.0001)) - 1.0) * 8.0, 0.0, 18.0)
    base_score = _clamp(gap_score + spike_score + (average_rms * 95.0), 0.0, 100.0)
    suspicion_score = _clamp((0.55 * base_score) + (0.45 * change_score), 0.0, 100.0)
    probability_dict = build_fast_voice_probability_dict(suspicion_score)
    probabilities_data = [probability_dict[label] for label in VOICE_LABELS]
    top_label = max(probability_dict, key=probability_dict.get)

    return {
        "segment_index": 0,
        "label": top_label,
        "confidence": probability_dict[top_label],
        "change_score": change_score,
        "base_score": base_score,
        "suspicion_score": suspicion_score,
        "status_text": get_voice_status_text(suspicion_score),
        "prob_dict": probability_dict,
        "probs": probabilities_data,
    }


def calculate_rms_windows(waveform: list[float], sample_rate: int) -> list[float]:
    """Calculates RMS values for short overlapping windows."""
    window_sample_count = max(1, math.ceil(sample_rate * FAST_WINDOW_SECONDS))
    stride_sample_count = max(1, math.ceil(sample_rate * FAST_STRIDE_SECONDS))

    if len(waveform) <= window_sample_count:
        return [calculate_window_rms(waveform, 0, len(waveform))]

    rms_values: list[float] = []
    start_sample_index = 0

    while start_sample_index + window_sample_count <= len(waveform):
        rms_values.append(calculate_window_rms(waveform, start_sample_index, window_sample_count))
        start_sample_index += stride_sample_count

    return rms_values


def has_sustained_voice_evidence(waveform: list[float], sample_rate: int) -> bool:
    """Returns whether one waveform contains enough sustained voice evidence."""
    rms_values = calculate_rms_windows(waveform, sample_rate)
    speech_rms_values = [rms_value for rms_value in rms_values if rms_value >= FAST_SPEECH_EVIDENCE_RMS_THRESHOLD]

    if len(speech_rms_values) < MINIMUM_FAST_SPEECH_EVIDENCE_WINDOW_COUNT:
        return False

    return max(speech_rms_values) >= FAST_SPEECH_EVIDENCE_PEAK_RMS_THRESHOLD


def build_fast_voice_probability_dict(suspicion_score: float) -> dict[str, float]:
    """Maps one acoustic instability score onto the existing voice labels."""
    tension = _clamp(suspicion_score / 100.0, 0.05, 0.85)
    stable = _clamp(1.0 - tension, 0.10, 0.80)
    medium = _clamp(1.0 - stable - tension, 0.05, 0.30)
    raw_probabilities = {
        "ang": tension * 0.42,
        "hap": stable * 0.22,
        "exc": medium * 0.55,
        "neu": stable * 0.78,
        "sad": medium * 0.45,
        "fru": tension * 0.58,
    }
    probability_sum = sum(raw_probabilities.values())
    return {label: raw_probabilities[label] / probability_sum for label in VOICE_LABELS}


def build_empty_voice_analysis() -> dict[str, Any]:
    """Builds one empty voice-analysis payload."""
    return {
        "audio_path": "",
        "segment_count": 0,
        "segments": [],
        "summary": summarize_voice_session([]),
    }


def split_audio_into_segments(waveform: list[float], sample_rate: int, segment_seconds: float = SEGMENT_SECONDS, stride_seconds: float = SEGMENT_STRIDE_SECONDS) -> list[list[float]]:
    """Splits one waveform into overlapping analysis segments."""
    segment_length = int(segment_seconds * sample_rate)
    stride_length = int(stride_seconds * sample_rate)

    if len(waveform) <= segment_length:
        return [waveform]

    segments: list[list[float]] = []
    start_index = 0

    while start_index + segment_length <= len(waveform):
        end_index = start_index + segment_length
        segments.append(waveform[start_index:end_index])
        start_index += stride_length

    if start_index < len(waveform):
        tail_segment = waveform[-segment_length:]

        if tail_segment:
            segments.append(tail_segment)

    return segments


def select_representative_segments(segments: list[list[float]], maximum_segment_count: int = MAX_ANALYSIS_SEGMENT_COUNT) -> list[list[float]]:
    """Selects evenly spaced speech segments so verdict latency stays bounded."""
    if maximum_segment_count <= 0:
        raise ValueError("maximum_segment_count must be greater than zero.")

    if len(segments) <= maximum_segment_count:
        return segments

    if maximum_segment_count == 1:
        return [segments[len(segments) // 2]]

    last_segment_index = len(segments) - 1
    selected_indices = {round((last_segment_index * sample_index) / (maximum_segment_count - 1)) for sample_index in range(maximum_segment_count)}
    return [segments[segment_index] for segment_index in sorted(selected_indices)]


def predict_voice_segment(feature_extractor: Any, model: Any, segment_waveform: list[float]) -> dict[str, Any]:
    """Runs one voice-emotion prediction on one waveform segment."""
    inputs = feature_extractor(segment_waveform, sampling_rate=TARGET_SAMPLE_RATE, return_tensors="pt", padding=True)

    with torch.no_grad():
        logits = model(**inputs).logits
        probabilities = torch.softmax(logits, dim=-1)[0]

    probabilities_data = probabilities.tolist()
    probability_dict = probs_to_dict(probabilities_data)
    top_index = int(torch.argmax(probabilities).item())
    top_label = list(probability_dict.keys())[top_index]

    return {
        "label": top_label,
        "confidence": float(probabilities[top_index].item()),
        "probs": probabilities_data,
        "prob_dict": probability_dict,
    }


def build_average_distribution(history: deque[list[float]]) -> list[float]:
    """Builds one average probability distribution from recent voice history."""
    average_distribution = [0.0] * len(history[0])

    for history_probabilities in history:
        for probability_index, probability_value in enumerate(history_probabilities):
            average_distribution[probability_index] += probability_value

    for probability_index in range(len(average_distribution)):
        average_distribution[probability_index] /= len(history)

    return average_distribution
