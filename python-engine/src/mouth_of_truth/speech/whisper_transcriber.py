"""Optional Whisper-based answer transcription support."""

from __future__ import annotations

from pathlib import Path
from typing import Any

import librosa
import torch
from transformers import AutoModelForSpeechSeq2Seq, AutoProcessor, pipeline

from mouth_of_truth.audio_signal import has_speech_signal
from mouth_of_truth.runtime.model_paths import resolve_whisper_model_cache_directory


WHISPER_MODEL_NAME = "openai/whisper-tiny"
WHISPER_SAMPLE_RATE = 16000


class WhisperTranscriber:
    """Transcribes one audio file with a cached Whisper pipeline."""

    def __init__(self, model_name: str = WHISPER_MODEL_NAME, cache_directory: str | Path | None = None) -> None:
        self._model_name = model_name
        self._cache_directory = Path(cache_directory).expanduser().resolve() if cache_directory is not None else resolve_whisper_model_cache_directory()
        self._transcription_pipeline = None

    def transcribe_audio_file(self, audio_file_path: str | Path, language_hint: str | None = None) -> str:
        """Returns the recognized transcript for one audio file."""
        resolved_audio_file_path = Path(audio_file_path).expanduser().resolve()

        if resolved_audio_file_path.exists() is False:
            raise FileNotFoundError(f"Speech audio file not found: {resolved_audio_file_path}")

        waveform, _ = librosa.load(resolved_audio_file_path, sr=WHISPER_SAMPLE_RATE, mono=True)

        if has_speech_signal(waveform.tolist(), WHISPER_SAMPLE_RATE) is False:
            return ""

        transcription = self._get_transcription_pipeline()(waveform, generate_kwargs=self._build_generate_kwargs(language_hint))
        return str(transcription.get("text", "")).strip()

    def _build_generate_kwargs(self, language_hint: str | None) -> dict[str, str]:
        """Builds generation keyword arguments for one optional language hint."""
        generate_kwargs = {"task": "transcribe"}

        if language_hint:
            generate_kwargs["language"] = language_hint

        return generate_kwargs

    def _get_transcription_pipeline(self) -> Any:
        """Returns the cached Hugging Face speech-recognition pipeline."""
        if self._transcription_pipeline is not None:
            return self._transcription_pipeline

        local_files_only = self._is_model_cached()
        processor = AutoProcessor.from_pretrained(self._model_name, cache_dir=self._cache_directory, local_files_only=local_files_only)
        model = AutoModelForSpeechSeq2Seq.from_pretrained(self._model_name, cache_dir=self._cache_directory, local_files_only=local_files_only)
        model.eval()

        self._transcription_pipeline = pipeline(task="automatic-speech-recognition", model=model, tokenizer=processor.tokenizer, feature_extractor=processor.feature_extractor, device=self._get_pipeline_device(), dtype=self._get_torch_dtype())

        return self._transcription_pipeline

    def _get_pipeline_device(self) -> int | torch.device:
        """Returns the best available local inference device."""
        if torch.cuda.is_available():
            return 0

        if getattr(torch.backends, "mps", None) is not None and torch.backends.mps.is_available():
            return torch.device("mps")

        return -1

    def _get_torch_dtype(self) -> torch.dtype:
        """Returns the preferred tensor dtype for the selected runtime."""
        return torch.float16 if torch.cuda.is_available() else torch.float32

    def _is_model_cached(self) -> bool:
        """Returns whether the requested Whisper model already exists locally."""
        model_cache_directory = Path(self._cache_directory) / f"models--{self._model_name.replace('/', '--')}" / "snapshots"
        return model_cache_directory.exists()
