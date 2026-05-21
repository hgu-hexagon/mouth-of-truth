"""Face-frame directory analysis pipeline for Unity answer captures."""

from __future__ import annotations

from collections import deque
from pathlib import Path
from typing import Any

import cv2

from mouth_of_truth.face.face_score_logic import calculate_base_score, calculate_change_score, calculate_suspicion_score, get_average_distribution, summarize_session
from mouth_of_truth.face.infer_face import load_face_model, predict_face_crop


FACE_PADDING = 20
HISTORY_SIZE = 15
MAX_ANALYSIS_FRAME_COUNT = 3
TARGET_ANALYSIS_RECOGNITION_COUNT = 1


def analyze_face_frame_directory(face_frames_directory_path: str | Path) -> dict[str, Any]:
    """Analyzes one saved face-frame directory and returns one session summary."""
    frame_files = list_frame_files(face_frames_directory_path)

    if not frame_files:
        return build_empty_face_analysis()

    sampled_frame_files = select_representative_frame_files(frame_files)

    cascade_file_path = cv2.data.haarcascades + "haarcascade_frontalface_default.xml"
    face_cascade = cv2.CascadeClassifier(cascade_file_path)

    if face_cascade.empty():
        raise RuntimeError("Failed to load the OpenCV frontal-face cascade.")

    face_model = load_face_model()
    history: deque[list[float]] = deque(maxlen=HISTORY_SIZE)
    recognition_results: list[dict[str, Any]] = []

    for frame_file_path in sampled_frame_files:
        frame = cv2.imread(str(frame_file_path))

        if frame is None:
            continue

        face_crop = extract_largest_face_crop(frame, face_cascade)

        if face_crop is None:
            continue

        prediction = predict_face_crop(face_model, face_crop)
        probabilities_data = prediction["probs"]
        history.append(probabilities_data)
        average_distribution = get_average_distribution(history)
        change_score = calculate_change_score(probabilities_data, average_distribution)
        base_score = calculate_base_score(prediction["prob_dict"])
        suspicion_score = calculate_suspicion_score(base_score, change_score)

        recognition_results.append({"label": prediction["label"], "conf": prediction["confidence"], "change_score": change_score, "base_score": base_score, "suspicion_score": suspicion_score})

        if len(recognition_results) >= TARGET_ANALYSIS_RECOGNITION_COUNT:
            break

    return {
        "frame_count": len(frame_files),
        "recognition_count": len(recognition_results),
        "summary": summarize_session(recognition_results),
    }


def build_empty_face_analysis() -> dict[str, Any]:
    """Builds one empty face-analysis payload."""
    return {
        "frame_count": 0,
        "recognition_count": 0,
        "summary": summarize_session([]),
    }


def list_frame_files(face_frames_directory_path: str | Path) -> list[Path]:
    """Lists one saved frame directory in stable sorted order."""
    face_frames_directory_path = Path(face_frames_directory_path)

    if face_frames_directory_path.exists() is False:
        return []

    frame_files = list(face_frames_directory_path.glob("*.jpg"))
    frame_files.extend(face_frames_directory_path.glob("*.jpeg"))
    frame_files.extend(face_frames_directory_path.glob("*.png"))
    return sorted(frame_files)


def select_representative_frame_files(frame_files: list[Path], maximum_frame_count: int = MAX_ANALYSIS_FRAME_COUNT) -> list[Path]:
    """Selects evenly spaced frames so long answers remain quick to analyze."""
    if maximum_frame_count <= 0:
        raise ValueError("maximum_frame_count must be greater than zero.")

    if len(frame_files) <= maximum_frame_count:
        return frame_files

    if maximum_frame_count == 1:
        return [frame_files[len(frame_files) // 2]]

    last_frame_index = len(frame_files) - 1
    selected_indices = {round((last_frame_index * sample_index) / (maximum_frame_count - 1)) for sample_index in range(maximum_frame_count)}
    return [frame_files[frame_index] for frame_index in sorted(selected_indices)]


def extract_largest_face_crop(frame: Any, face_cascade: cv2.CascadeClassifier) -> Any | None:
    """Extracts the largest face crop from one saved frame."""
    grayscale_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    faces = face_cascade.detectMultiScale(grayscale_frame, scaleFactor=1.1, minNeighbors=5, minSize=(80, 80))

    if len(faces) == 0:
        return None

    x, y, width, height = max(faces, key=lambda rect: rect[2] * rect[3])
    x1 = max(0, x - FACE_PADDING)
    y1 = max(0, y - FACE_PADDING)
    x2 = min(frame.shape[1], x + width + FACE_PADDING)
    y2 = min(frame.shape[0], y + height + FACE_PADDING)
    return frame[y1:y2, x1:x2]
