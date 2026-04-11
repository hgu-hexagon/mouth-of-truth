from __future__ import annotations

from collections import deque
from typing import Any

import torch

from mouth_of_truth.voice.infer_voice import (
    TARGET_SAMPLE_RATE,
    load_audio,
    load_voice_model,
    probs_to_dict,
)
from mouth_of_truth.voice.voice_score_logic import (
    calculate_voice_base_score,
    calculate_voice_change_score,
    calculate_voice_suspicion_score,
    get_voice_status_text,
    summarize_voice_session,
)


SEGMENT_SECONDS = 2.0
SEGMENT_STRIDE_SECONDS = 1.0
VOICE_HISTORY_SIZE = 10


def run_voice_emotion_pipeline(audio_path: str) -> dict[str, Any]:
    """Runs one voice-emotion analysis pipeline on one recorded answer file."""
    feature_extractor, model = load_voice_model()
    waveform = load_audio(audio_path)
    segments = split_audio_into_segments(waveform, TARGET_SAMPLE_RATE)
    history: deque[list[float]] = deque(maxlen=VOICE_HISTORY_SIZE)
    segment_results: list[dict[str, Any]] = []

    for segment_index, segment_waveform in enumerate(segments):
        prediction = predict_voice_segment(feature_extractor, model, segment_waveform)
        probabilities_data = prediction["probs"]
        history.append(probabilities_data)
        average_distribution = build_average_distribution(history)
        change_score = calculate_voice_change_score(probabilities_data, average_distribution)
        base_score = calculate_voice_base_score(prediction["prob_dict"])
        suspicion_score = calculate_voice_suspicion_score(base_score, change_score)

        segment_results.append(
            {
                "segment_index": segment_index,
                "label": prediction["label"],
                "confidence": prediction["confidence"],
                "change_score": change_score,
                "base_score": base_score,
                "suspicion_score": suspicion_score,
                "status_text": get_voice_status_text(suspicion_score),
                "prob_dict": prediction["prob_dict"],
            }
        )

    return {
        "audio_path": audio_path,
        "segment_count": len(segment_results),
        "segments": segment_results,
        "summary": summarize_voice_session(segment_results),
    }


def build_empty_voice_analysis() -> dict[str, Any]:
    """Builds one empty voice-analysis payload."""
    return {
        "audio_path": "",
        "segment_count": 0,
        "segments": [],
        "summary": summarize_voice_session([]),
    }


def split_audio_into_segments(
    waveform: list[float],
    sample_rate: int,
    segment_seconds: float = SEGMENT_SECONDS,
    stride_seconds: float = SEGMENT_STRIDE_SECONDS,
) -> list[list[float]]:
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


def predict_voice_segment(
    feature_extractor: Any,
    model: Any,
    segment_waveform: list[float],
) -> dict[str, Any]:
    """Runs one voice-emotion prediction on one waveform segment."""
    inputs = feature_extractor(
        segment_waveform,
        sampling_rate=TARGET_SAMPLE_RATE,
        return_tensors="pt",
        padding=True,
    )

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
