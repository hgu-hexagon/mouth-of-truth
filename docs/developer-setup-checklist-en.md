# Mouth of Truth Developer Setup Guide

## Overview

This document explains how to set up the Mouth of Truth project on a new
machine after cloning the repository from GitHub.

The target audience is a developer who works with both the Unity app and the
Python analysis runtime.

After completing this guide, you should be able to:

- clone the repository
- create the Python runtime environment
- open the Unity project from the correct path
- run the main scene
- verify the baseline game flow in deterministic mode
- verify the Python bridge and analysis path

## Prerequisites

Install the following tools first.

- Git
- Unity Hub
- Unity Editor `6000.4.1f1`
- Miniforge or Mambaforge

The current project has been validated primarily on macOS.

## Clone the repository

Clone the repository into your preferred workspace.

```bash
git clone https://github.com/hgu-hexagon/mouth-of-truth.git
cd mouth-of-truth
```

In this guide, the current working directory is called `<repo-root>`.

## Project structure

The two important roots in this repository are:

- Unity app:
  - `<repo-root>/unity-app`
- Python engine:
  - `<repo-root>/python-engine`

Important:

- Do not open the repository root directly in Unity.
- Open the Unity project from `unity-app/` only.

## Create the Python environment

### 1. Create the conda environment

Create the project environment with:

```bash
conda env create -f python-engine/environment.yml
```

The default environment name is:

- `mouth-of-truth`

### 2. Activate the environment

```bash
conda activate mouth-of-truth
```

### 3. Check the dependency manifests

The project uses these files as the Python dependency baseline:

- `python-engine/environment.yml`
- `python-engine/requirements.txt`

`environment.yml` defines the environment bootstrap.
`requirements.txt` defines the core runtime packages.

## Open the Unity project

### 1. Open the project in Unity Hub

Open this path:

- `<repo-root>/unity-app`

### 2. Verify the Unity version

The project baseline is:

- `6000.4.1f1`

### 3. Open the main scene

Open this scene in Unity:

- `Assets/Scenes/Main.unity`

## Prepare the local analysis models

### Overview

The project uses local assets for face analysis, voice emotion analysis, and
Whisper transcription.

These model binaries are not committed to Git.
To run the full analysis path, you must populate the local model directories.

### Required paths

The full analysis path requires these locations:

- Face model:
  - `python-engine/models/face/yolo26x_rafdb_best.pt`
- Voice emotion model:
  - `python-engine/models/voice/best_wav2vec2_iemocap/`
- Whisper cache:
  - `python-engine/models/whisper/models--openai--whisper-tiny/`

## Execution modes

The project supports two execution modes.

### Deterministic mode

This mode verifies the baseline game flow without requiring local analysis
models.

Use this mode to verify:

- the title screen
- card selection
- question reveal
- TTS
- answer collection
- result presentation

This mode generates analysis results from deterministic rules.

### Python analysis mode

This mode uses the real Python analysis pipeline, including microphone input,
face frame capture, Whisper transcription, and face/voice analysis.

This mode requires:

- the Python environment
- local model assets
- microphone access
- webcam access

## Run the project in deterministic mode

### 1. Launch Unity with the deterministic flag

Start Unity with this environment variable:

```bash
cd <repo-root>
MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic open -a "Unity" unity-app
```

If the project is already open in Unity, restarting Unity with the environment
variable is the safest option.

### 2. Press Play in the main scene

The baseline run is successful when the following flow works:

- the title screen appears
- three question cards appear
- hover and dwell card selection works
- the selected question is revealed
- TTS reads the question
- answer collection starts
- a result screen appears

## Run the project in Python analysis mode

### 1. Populate the local model assets

Make sure these paths exist:

- `python-engine/models/face/yolo26x_rafdb_best.pt`
- `python-engine/models/voice/best_wav2vec2_iemocap/`
- `python-engine/models/whisper/models--openai--whisper-tiny/`

### 2. Verify the Python sources

Run this command from the repository root:

```bash
cd <repo-root>
conda activate mouth-of-truth
python -m compileall python-engine/src
```

### 3. Verify the bridge runtime

Run this command from the repository root:

```bash
cd <repo-root>
python-engine/scripts/validate_bridge_runtime.sh
```

This validation checks:

- request JSON generation
- Python runner execution
- Whisper transcription
- result JSON generation

### 4. Press Play in Unity

Without the deterministic override, the project will prefer the Python analysis
path.

## Verification checklist

### Deterministic mode

The baseline deterministic setup is complete when all of the following are true.

- The Unity project opens from `unity-app/`.
- `Assets/Scenes/Main.unity` opens correctly.
- Unity has no blocking compile errors.
- Card selection works.
- TTS plays.
- The result screen appears.

### Python analysis mode

The Python analysis path is ready when all of the following are true.

- `python -m compileall python-engine/src` passes.
- The face model path exists.
- The voice model path exists.
- The Whisper cache path exists.
- `python-engine/scripts/validate_bridge_runtime.sh` passes.

## Troubleshooting

### Unity opens a blank default scene

Cause:

- The repository root was opened as a Unity project by mistake.

Action:

- Close Unity.
- Open `<repo-root>/unity-app` only.

### Python cannot find the models

Cause:

- The local assets under `python-engine/models/` may be missing.

Action:

- Recheck the face, voice, and Whisper cache paths.
- If needed, set `MOUTH_OF_TRUTH_MODELS_ROOT` explicitly.

### Whisper tries to download files

Cause:

- The project-local Whisper cache may be missing or incomplete.

Action:

- Populate `python-engine/models/whisper/models--openai--whisper-tiny/`.
- Use `TRANSFORMERS_OFFLINE=1` and `HF_HUB_OFFLINE=1` during offline checks.

### Unity cannot launch Python analysis

Cause:

- The Python launcher cannot find a usable runtime.

Action:

- Verify that `conda env create -f python-engine/environment.yml` completed successfully.
- Verify that `conda activate mouth-of-truth` works.
- If needed, set `MOUTH_OF_TRUTH_PYTHON` to the exact Python executable path.
- For release packages, verify that the `python-runtime/` folder is included.

## Related documents

- `README.md`
- `docs/build-and-distribution-guide-en.md`
- `python-engine/environment.yml`
- `python-engine/requirements.txt`
- `python-engine/models/README.md`
- `unity-app/Packages/manifest.json`
