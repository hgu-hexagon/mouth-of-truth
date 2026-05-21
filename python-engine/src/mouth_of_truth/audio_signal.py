"""Shared audio-signal helpers for speech evidence detection."""

from __future__ import annotations

import math


DEFAULT_SPEECH_WINDOW_SECONDS = 0.20
DEFAULT_SPEECH_RMS_THRESHOLD = 0.0085


def has_speech_signal(waveform: list[float], sample_rate: int, window_seconds: float = DEFAULT_SPEECH_WINDOW_SECONDS, rms_threshold: float = DEFAULT_SPEECH_RMS_THRESHOLD) -> bool:
    """Returns whether one waveform contains one speech-like signal window."""
    if not waveform:
        return False

    window_sample_count = max(1, math.ceil(sample_rate * window_seconds))
    stride_sample_count = max(1, window_sample_count // 2)

    if len(waveform) <= window_sample_count:
        return calculate_window_rms(waveform, 0, len(waveform)) >= rms_threshold

    start_sample_index = 0

    while start_sample_index + window_sample_count <= len(waveform):
        if calculate_window_rms(waveform, start_sample_index, window_sample_count) >= rms_threshold:
            return True

        start_sample_index += stride_sample_count

    tail_window_start_index = max(0, len(waveform) - window_sample_count)
    tail_sample_count = len(waveform) - tail_window_start_index
    return calculate_window_rms(waveform, tail_window_start_index, tail_sample_count) >= rms_threshold


def calculate_window_rms(waveform: list[float], start_sample_index: int, sample_count: int) -> float:
    """Calculates one RMS value for one waveform window."""
    if sample_count <= 0:
        return 0.0

    squared_sum = 0.0

    for sample_index in range(sample_count):
        sample_value = waveform[start_sample_index + sample_index]
        squared_sum += sample_value * sample_value

    mean_square = squared_sum / sample_count
    return math.sqrt(mean_square)
