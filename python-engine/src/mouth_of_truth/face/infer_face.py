"""Face-emotion model loading and inference helpers."""

from __future__ import annotations

from typing import Any

from ultralytics import YOLO

from mouth_of_truth.runtime.model_paths import resolve_face_model_path


_CACHED_FACE_MODEL: YOLO | None = None


def load_face_model() -> YOLO:
    """Loads one trained face-emotion model."""
    global _CACHED_FACE_MODEL

    if _CACHED_FACE_MODEL is not None:
        return _CACHED_FACE_MODEL

    face_model_path = resolve_face_model_path()
    _CACHED_FACE_MODEL = YOLO(str(face_model_path))
    return _CACHED_FACE_MODEL


def probs_to_dict(model_names: dict[int, str], probs_data: list[float]) -> dict[str, float]:
    """Converts one probability list into one label-to-score dictionary."""
    probability_dict: dict[str, float] = {}

    for class_index, class_name in model_names.items():
        probability_dict[class_name] = float(probs_data[class_index])

    return probability_dict


def predict_face_crop(model: YOLO, face_crop: Any) -> dict[str, Any]:
    """Runs face-emotion prediction on one cropped face image."""
    prediction_results = model.predict(face_crop, verbose=False)

    if not prediction_results:
        raise RuntimeError("No prediction result returned from the face model.")

    prediction_result = prediction_results[0]
    probabilities = prediction_result.probs

    if probabilities is None:
        raise RuntimeError("The face model did not return classification probabilities.")

    top_index = int(probabilities.top1)
    top_confidence = float(probabilities.top1conf)
    top_label = model.names[top_index]
    probabilities_data = probabilities.data.tolist()

    return {
        "label": top_label,
        "confidence": top_confidence,
        "probs": probabilities_data,
        "prob_dict": probs_to_dict(model.names, probabilities_data),
    }
