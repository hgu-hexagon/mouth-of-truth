"""Model path resolution for local and packaged Python runtimes."""

from __future__ import annotations

import os
from pathlib import Path


FACE_MODEL_RELATIVE_PATH = Path("face") / "yolo26x_rafdb_best.pt"
VOICE_MODEL_RELATIVE_PATH = Path("voice") / "best_wav2vec2_iemocap"
WHISPER_MODEL_CACHE_RELATIVE_PATH = Path("whisper")
MODELS_ROOT_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_MODELS_ROOT"


def resolve_face_model_path() -> Path:
    """Resolves the face-emotion model path from the known model roots."""
    return resolve_model_path(FACE_MODEL_RELATIVE_PATH)


def resolve_voice_model_directory() -> Path:
    """Resolves the voice-emotion model directory from the known model roots."""
    return resolve_model_path(VOICE_MODEL_RELATIVE_PATH)


def resolve_whisper_model_cache_directory() -> Path:
    """Resolves the local Whisper cache directory from the known model roots."""
    return resolve_model_path(WHISPER_MODEL_CACHE_RELATIVE_PATH)


def resolve_model_path(relative_model_path: Path) -> Path:
    """Resolves one model path from the current project or one explicit override."""
    searched_paths: list[Path] = []

    for models_root_path in build_candidate_model_roots():
        candidate_path = models_root_path / relative_model_path
        searched_paths.append(candidate_path)

        if candidate_path.exists():
            return candidate_path

    searched_paths_text = "\n".join(str(path) for path in searched_paths)
    raise FileNotFoundError("Model asset not found. Searched:\n" f"{searched_paths_text}")


def build_candidate_model_roots() -> list[Path]:
    """Builds the ordered list of candidate model roots."""
    candidate_model_roots: list[Path] = []
    configured_models_root = os.environ.get(MODELS_ROOT_ENVIRONMENT_VARIABLE_NAME, "").strip()

    if configured_models_root:
        candidate_model_roots.append(Path(configured_models_root))

    candidate_model_roots.append(get_local_models_root())

    unique_model_roots: list[Path] = []

    for candidate_model_root in candidate_model_roots:
        normalized_candidate_model_root = candidate_model_root.expanduser().resolve()

        if normalized_candidate_model_root in unique_model_roots:
            continue

        unique_model_roots.append(normalized_candidate_model_root)

    return unique_model_roots


def get_local_models_root() -> Path:
    """Returns the current project's local models directory."""
    return get_python_engine_root() / "models"


def get_project_root() -> Path:
    """Returns the current repository root."""
    return get_python_engine_root().parent


def get_python_engine_root() -> Path:
    """Returns the current Python engine root."""
    return Path(__file__).resolve().parents[3]
