"""Voice-emotion model loading and audio inference helpers."""

from __future__ import annotations

from array import array
from pathlib import Path
import sys
from typing import Any
import wave

import librosa
import torch
from transformers import AutoFeatureExtractor, AutoModelForAudioClassification

from mouth_of_truth.runtime.model_paths import resolve_voice_model_directory


VOICE_LABELS = ["ang", "hap", "exc", "neu", "sad", "fru"]
TARGET_SAMPLE_RATE = 16000
_CACHED_FEATURE_EXTRACTOR: AutoFeatureExtractor | None = None
_CACHED_VOICE_MODEL: AutoModelForAudioClassification | None = None


def load_voice_model() -> tuple[AutoFeatureExtractor, AutoModelForAudioClassification]:
    """Loads one trained voice-emotion model and feature extractor."""
    global _CACHED_FEATURE_EXTRACTOR
    global _CACHED_VOICE_MODEL

    if _CACHED_FEATURE_EXTRACTOR is not None and _CACHED_VOICE_MODEL is not None:
        return _CACHED_FEATURE_EXTRACTOR, _CACHED_VOICE_MODEL

    voice_model_directory = resolve_voice_model_directory()
    voice_model_directory_path = str(voice_model_directory)
    _CACHED_FEATURE_EXTRACTOR = AutoFeatureExtractor.from_pretrained(voice_model_directory_path)
    _CACHED_VOICE_MODEL = AutoModelForAudioClassification.from_pretrained(voice_model_directory_path)
    _CACHED_VOICE_MODEL.eval()
    return _CACHED_FEATURE_EXTRACTOR, _CACHED_VOICE_MODEL


def load_audio(audio_path: str) -> list[float]:
    """Loads one audio file and resamples it to the target sample rate."""
    audio_file_path = Path(audio_path)

    if audio_file_path.exists() is False:
        raise FileNotFoundError(f"Audio file not found: {audio_path}")

    pcm_waveform = load_target_pcm_wav(audio_file_path)

    if pcm_waveform is not None:
        return pcm_waveform

    waveform, _ = librosa.load(audio_file_path, sr=TARGET_SAMPLE_RATE, mono=True)
    return waveform.tolist()


def load_target_pcm_wav(audio_file_path: Path) -> list[float] | None:
    """Loads one target-format PCM wav file without the heavier librosa path."""
    try:
        with wave.open(str(audio_file_path), "rb") as wave_file:
            if wave_file.getnchannels() != 1 or wave_file.getsampwidth() != 2 or wave_file.getframerate() != TARGET_SAMPLE_RATE:
                return None

            raw_frames = wave_file.readframes(wave_file.getnframes())
    except wave.Error:
        return None

    samples = array("h")
    samples.frombytes(raw_frames)

    if sys.byteorder == "big":
        samples.byteswap()

    return [sample / 32768.0 for sample in samples]


def probs_to_dict(probs_data: list[float]) -> dict[str, float]:
    """Converts one voice probability list into one label-to-score dictionary."""
    if len(probs_data) != len(VOICE_LABELS):
        raise ValueError(f"Unexpected number of voice class probabilities: {len(probs_data)} (expected {len(VOICE_LABELS)})")

    return {label: float(score) for label, score in zip(VOICE_LABELS, probs_data)}


def predict_voice_file(feature_extractor: AutoFeatureExtractor, model: AutoModelForAudioClassification, audio_path: str) -> dict[str, Any]:
    """Runs one full-file voice-emotion prediction."""
    waveform = load_audio(audio_path)
    inputs = feature_extractor(waveform, sampling_rate=TARGET_SAMPLE_RATE, return_tensors="pt", padding=True)

    with torch.no_grad():
        logits = model(**inputs).logits
        probabilities = torch.softmax(logits, dim=-1)[0]

    probabilities_data = probabilities.tolist()
    probability_dict = probs_to_dict(probabilities_data)
    top_index = int(torch.argmax(probabilities).item())

    return {
        "audio_path": audio_path,
        "label": VOICE_LABELS[top_index],
        "confidence": float(probabilities[top_index].item()),
        "probs": probabilities_data,
        "prob_dict": probability_dict,
    }
