from __future__ import annotations

import sys
from pathlib import Path

from mouth_of_truth.contracts.analysis_contracts import (
    AnalysisRequest,
    AnalysisResult,
    read_analysis_request,
    write_analysis_result,
)
from mouth_of_truth.contracts.verdict_kind import VerdictKind
from mouth_of_truth.speech.whisper_transcriber import WhisperTranscriber


MINIMUM_FACE_RECOGNITION_COUNT = 5
MINIMUM_VOICE_SEGMENT_COUNT = 1


def build_analysis_result(analysis_request: AnalysisRequest) -> AnalysisResult:
    """Builds one verdict result from one request payload."""
    answer_transcript = analysis_request.answer_transcript.strip()

    if not answer_transcript and analysis_request.answer_audio_file_path.strip():
        whisper_transcriber = WhisperTranscriber()
        language_hint = detect_language_hint(analysis_request.question_text)
        answer_transcript = whisper_transcriber.transcribe_audio_file(
            analysis_request.answer_audio_file_path,
            language_hint=language_hint,
        ).strip()

    reason_codes: list[str] = []

    if analysis_request.face_recognition_count < MINIMUM_FACE_RECOGNITION_COUNT:
        reason_codes.append("insufficient_face_data")

    if analysis_request.voice_segment_count < MINIMUM_VOICE_SEGMENT_COUNT or not answer_transcript:
        reason_codes.append("insufficient_voice_data")

    if reason_codes:
        return AnalysisResult(
            request_id=analysis_request.request_id,
            verdict=VerdictKind.UNCERTAIN,
            answer_transcript=answer_transcript,
            reason_codes=reason_codes,
        )

    parity_seed = calculate_stable_parity_seed(
        analysis_request.question_id,
        answer_transcript,
    )
    verdict = VerdictKind.TRUE if parity_seed % 2 == 0 else VerdictKind.FALSE

    return AnalysisResult(
        request_id=analysis_request.request_id,
        verdict=verdict,
        answer_transcript=answer_transcript,
    )


def run_once(request_file_path: str | Path, result_file_path: str | Path) -> None:
    """Reads one request file and writes one result file."""
    analysis_request = read_analysis_request(request_file_path)
    analysis_result = build_analysis_result(analysis_request)
    write_analysis_result(result_file_path, analysis_result)


def calculate_stable_parity_seed(question_id: str, answer_transcript: str) -> int:
    """Builds one deterministic parity seed from the request transcript."""
    checksum = 0

    for character in f"{question_id}|{answer_transcript.strip()}":
        checksum += ord(character)

    return checksum


def detect_language_hint(question_text: str) -> str | None:
    """Infers a Whisper language hint from the visible question text."""
    normalized_question_text = question_text.strip()

    if not normalized_question_text:
        return None

    if any("\uac00" <= character <= "\ud7a3" for character in normalized_question_text):
        return "ko"

    if normalized_question_text.isascii():
        return "en"

    return None


def main(argv: list[str] | None = None) -> int:
    """Runs one bridge analysis job from CLI arguments."""
    argv = list(sys.argv[1:] if argv is None else argv)

    if len(argv) != 2:
        print(
            "Usage: python -m mouth_of_truth.runners.bridge_analysis_runner "
            "<request-file-path> <result-file-path>",
            file=sys.stderr,
        )
        return 2

    request_file_path, result_file_path = argv
    run_once(request_file_path, result_file_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
