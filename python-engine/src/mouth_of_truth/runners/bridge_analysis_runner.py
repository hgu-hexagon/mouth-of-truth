"""One-shot bridge analysis runner used by Unity and packaging tests."""

from __future__ import annotations

import os
import sys
import traceback
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from typing import Any


def _ensure_package_root_on_sys_path() -> None:
    """Adds the python-engine src directory for direct script execution."""
    package_root_path = Path(__file__).resolve().parents[2]

    if str(package_root_path) not in sys.path:
        sys.path.insert(0, str(package_root_path))


_ensure_package_root_on_sys_path()

from mouth_of_truth.contracts.analysis_contracts import AnalysisRequest, AnalysisResult, read_analysis_request, write_analysis_result
from mouth_of_truth.fusion.judgment_policy import build_analysis_result as build_fused_analysis_result


AnalysisPayload = dict[str, Any]
ENABLE_TRANSCRIPTION_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_ENABLE_TRANSCRIPTION"


def _build_analysis_result(analysis_request: AnalysisRequest) -> AnalysisResult:
    """Builds one verdict result from one bridge request payload."""
    answer_transcript = analysis_request.answer_transcript.strip()

    if not answer_transcript and analysis_request.answer_audio_file_path.strip() and _should_transcribe_answer():
        from mouth_of_truth.speech.whisper_transcriber import WhisperTranscriber

        whisper_transcriber = WhisperTranscriber()
        language_hint = _detect_language_hint(analysis_request.question_text)
        answer_transcript = whisper_transcriber.transcribe_audio_file(analysis_request.answer_audio_file_path, language_hint=language_hint).strip()

    face_analysis, voice_analysis = _analyze_modalities(analysis_request)
    face_recognition_count = _resolve_face_recognition_count(analysis_request, face_analysis)
    voice_segment_count = _resolve_voice_segment_count(analysis_request, voice_analysis)

    return build_fused_analysis_result(request_id=analysis_request.request_id, answer_transcript=answer_transcript, face_result=face_analysis["summary"], voice_result=voice_analysis["summary"], face_recognition_count=face_recognition_count, voice_segment_count=voice_segment_count)


def _analyze_modalities(analysis_request: AnalysisRequest) -> tuple[AnalysisPayload, AnalysisPayload]:
    """Runs face and voice analysis in parallel to keep verdict latency low."""
    with ThreadPoolExecutor(max_workers=2) as executor:
        face_future = executor.submit(_analyze_face_data, analysis_request)
        voice_future = executor.submit(_analyze_voice_data, analysis_request)
        return face_future.result(), voice_future.result()


def run_once(request_file_path: str | Path, result_file_path: str | Path) -> None:
    """Reads one request file and writes one result file."""
    analysis_request = read_analysis_request(request_file_path)
    analysis_result = _build_analysis_result(analysis_request)
    write_analysis_result(result_file_path, analysis_result)


def _analyze_face_data(analysis_request: AnalysisRequest) -> AnalysisPayload:
    """Analyzes one saved face-frame directory, if it exists."""
    from mouth_of_truth.face.frame_directory_pipeline import analyze_face_frame_directory, build_empty_face_analysis

    face_frames_directory_path = analysis_request.face_frames_directory_path.strip()

    if not face_frames_directory_path:
        return build_empty_face_analysis()

    try:
        return analyze_face_frame_directory(face_frames_directory_path)
    except Exception as exception:
        print(f"Face analysis failed. Falling back to empty face data.\n{exception}\n{traceback.format_exc()}", file=sys.stderr)
        return build_empty_face_analysis()


def _analyze_voice_data(analysis_request: AnalysisRequest) -> AnalysisPayload:
    """Analyzes one saved answer audio file, if it exists."""
    from mouth_of_truth.voice.voice_emotion_pipeline import build_empty_voice_analysis, run_voice_emotion_pipeline

    answer_audio_file_path = analysis_request.answer_audio_file_path.strip()

    if not answer_audio_file_path:
        return build_empty_voice_analysis()

    try:
        return run_voice_emotion_pipeline(answer_audio_file_path)
    except Exception as exception:
        print(f"Voice analysis failed. Falling back to empty voice data.\n{exception}\n{traceback.format_exc()}", file=sys.stderr)
        return build_empty_voice_analysis()


def _resolve_face_recognition_count(analysis_request: AnalysisRequest, face_analysis: AnalysisPayload) -> int:
    """Resolves the face-recognition count used for judgment readiness."""
    if analysis_request.face_frames_directory_path.strip():
        return int(face_analysis.get("recognition_count", 0))

    return analysis_request.face_frame_count


def _resolve_voice_segment_count(analysis_request: AnalysisRequest, voice_analysis: AnalysisPayload) -> int:
    """Resolves the voice-segment count used for judgment readiness."""
    if analysis_request.answer_audio_file_path.strip():
        return int(voice_analysis.get("segment_count", 0))

    return analysis_request.voice_segment_count


def _detect_language_hint(question_text: str) -> str | None:
    """Infers a Whisper language hint from the visible question text."""
    normalized_question_text = question_text.strip()

    if not normalized_question_text:
        return None

    if any("\uac00" <= character <= "\ud7a3" for character in normalized_question_text):
        return "ko"

    if normalized_question_text.isascii():
        return "en"

    return None


def _should_transcribe_answer() -> bool:
    """Returns whether answer transcription should run before returning a verdict."""
    configured_value = os.environ.get(ENABLE_TRANSCRIPTION_ENVIRONMENT_VARIABLE_NAME, "")
    return configured_value.strip().lower() in {"1", "true", "yes", "on"}


def main(argv: list[str] | None = None) -> int:
    """Runs one bridge analysis job from CLI arguments."""
    argv = list(sys.argv[1:] if argv is None else argv)

    if len(argv) != 2:
        print("Usage: python -m mouth_of_truth.runners.bridge_analysis_runner <request-file-path> <result-file-path>", file=sys.stderr)
        return 2

    request_file_path, result_file_path = argv
    run_once(request_file_path, result_file_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
