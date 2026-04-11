# Mouth of Truth Developer Setup Checklist

## Overview

This document explains the minimum setup required to run the Mouth of Truth
project on a new development machine.

The target audience is a developer who needs to work with both the Unity app
and the Python analysis engine.

After completing this document, you should be able to:

- open the Unity project from the correct path
- run the Python analysis runtime from the current project
- place local model assets in the expected project structure
- open the main scene and verify the baseline compile state

## Prerequisites

Make sure the following conditions are met first.

- The operating system is macOS.
- Unity `6000.4.1f1` is installed.
- The existing `mouth-truth` conda environment is available.
- Git is installed.
- The repository is checked out locally.

Example project root:

- `/Users/potterlim/Developments/Projects/new-mouth-of-truth`

## Project structure

The two important roots in this repository are:

- Unity app:
  - `unity-app/`
- Python engine:
  - `python-engine/`

Important:

- Do not open the repository root directly in Unity.
- Open the Unity project from this path instead:
  - `/Users/potterlim/Developments/Projects/new-mouth-of-truth/unity-app`

## Python setup

### 1. Activate the conda environment

Activate the existing environment in a terminal.

```bash
source /Users/potterlim/Developments/Packages/miniforge/etc/profile.d/conda.sh
conda activate mouth-truth
```

### 2. Check the Python dependency manifest

The current Python dependency manifest is:

- `python-engine/requirements.txt`

This file defines the core packages used by the current runtime.

Key packages include:

- `torch`
- `torchaudio`
- `transformers`
- `huggingface_hub`
- `librosa`
- `soundfile`
- `opencv-python`
- `ultralytics`

### 3. Install or verify Python packages

The default approach is to reuse the existing `mouth-truth` environment.

If needed, align the environment with:

```bash
pip install -r /Users/potterlim/Developments/Projects/new-mouth-of-truth/python-engine/requirements.txt
```

## Local model assets

Model binaries are not committed to Git.
They must be present locally in the current project.

### Required paths

- Face model:
  - `python-engine/models/face/yolo26x_rafdb_best.pt`
- Voice emotion model:
  - `python-engine/models/voice/best_wav2vec2_iemocap/`
- Whisper cache:
  - `python-engine/models/whisper/models--openai--whisper-tiny/`

### Notes

- Runtime code no longer falls back to the old project path.
- The current project resolves models only from `python-engine/models/`.
- If needed, you can explicitly override the root with
  `MOUTH_OF_TRUTH_MODELS_ROOT`.

## Unity setup

### 1. Open the Unity project

Open this path in Unity Hub:

- `/Users/potterlim/Developments/Projects/new-mouth-of-truth/unity-app`

Do not open the repository root as a Unity project.

### 2. Verify the Unity version

The current Unity baseline is:

- `6000.4.1f1`

Opening the project with a different version can change package behavior and
serialized assets.

### 3. Verify the Unity package manifest

The Unity package manifest is:

- `unity-app/Packages/manifest.json`

The current critical packages include:

- `com.ultraleap.tracking`
- `com.unity.inputsystem`
- `com.unity.render-pipelines.universal`
- `com.unity.ugui`
- `com.unity.ai.navigation`

## Main scene

Open this scene in Unity:

- `Assets/Scenes/Main.unity`

The scene should contain at least:

- `MouthOfTruthApp`
- `Main Camera`
- `EventSystem`
- the main stage environment
- card and mouth anchor structures

## Python runtime verification

From the project root, verify that the Python sources compile:

```bash
source /Users/potterlim/Developments/Packages/miniforge/etc/profile.d/conda.sh
conda activate mouth-truth
cd /Users/potterlim/Developments/Projects/new-mouth-of-truth
python -m compileall python-engine/src
```

This command should complete without errors.

## Whisper offline cache verification

The current project uses a project-local Whisper cache instead of the global
Hugging Face cache.

Verify that this path exists:

- `python-engine/models/whisper/models--openai--whisper-tiny/`

Example offline verification:

```bash
source /Users/potterlim/Developments/Packages/miniforge/etc/profile.d/conda.sh
conda activate mouth-truth
cd /Users/potterlim/Developments/Projects/new-mouth-of-truth
TRANSFORMERS_OFFLINE=1 HF_HUB_OFFLINE=1 PYTHONPATH=python-engine/src python - <<'PY'
from mouth_of_truth.speech.whisper_transcriber import WhisperTranscriber
transcriber = WhisperTranscriber()
print(transcriber._cache_directory)
print(transcriber._is_model_cached())
PY
```

The output path should point to `python-engine/models/whisper` inside the
current project.

## Verification checklist

The baseline setup is complete when all of the following are true.

- The Unity project opens from `unity-app/`.
- `Assets/Scenes/Main.unity` opens correctly.
- Unity has no blocking compile errors.
- Python `compileall` passes.
- The face model path exists.
- The voice model path exists.
- The Whisper cache path exists.

## Troubleshooting

### Unity opens a blank default scene

Cause:

- The repository root was likely opened as a Unity project by mistake.

Action:

- Close Unity.
- Open only the `unity-app/` path again.

### Python cannot find the models

Cause:

- The local assets under `python-engine/models/` may be missing.

Action:

- Recheck the face, voice, and Whisper cache paths.
- If necessary, set `MOUTH_OF_TRUTH_MODELS_ROOT` explicitly.

### Whisper tries to download files

Cause:

- The project-local Whisper cache may be missing or incomplete.

Action:

- Populate `python-engine/models/whisper/models--openai--whisper-tiny/`.
- Use `TRANSFORMERS_OFFLINE=1` and `HF_HUB_OFFLINE=1` during offline checks.

## Related documents

- `python-engine/requirements.txt`
- `unity-app/Packages/manifest.json`
- `python-engine/models/README.md`
- `docs/session-architecture-ko.md`
