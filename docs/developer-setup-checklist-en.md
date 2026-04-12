# Mouth of Truth Developer Setup Guide

## Overview

This document explains how to set up the Mouth of Truth project on a fresh
machine after cloning the GitHub repository.

After completing this guide, you should be able to:

- clone the repository
- create the Python environment
- open the Unity project from the correct path
- run the baseline game flow in deterministic mode
- prepare the local model assets for full analysis
- run the Python bridge validation

## Prerequisites

Install the following tools first.

- Git
- Unity Hub
- Unity Editor `6000.4.1f1`
- Miniforge or Mambaforge

The current project has been validated primarily on macOS.

## Clone the repository

Run the following commands in your preferred workspace.

```bash
git clone https://github.com/hgu-hexagon/mouth-of-truth.git
cd mouth-of-truth
```

This guide refers to the current working directory as `<repo-root>`.

## Project structure

The two important project roots are:

- Unity app:
  - `<repo-root>/unity-app`
- Python engine:
  - `<repo-root>/python-engine`

Important:

- Open Unity from `unity-app/` only.
- Do not open the repository root directly as a Unity project.

## Create the Python environment

### 1. Create the conda environment

Run this command from the repository root.

```bash
conda env create -f python-engine/environment.yml
```

The default environment name is `mouth-of-truth`.

### 2. Activate the environment

```bash
conda activate mouth-of-truth
```

### 3. Verify the Python environment

```bash
python --version
python -m compileall python-engine/src
```

If these commands succeed, the Python sources are readable and compilable.

## Open the Unity project

### 1. Open the project in Unity Hub

Open this path:

- `<repo-root>/unity-app`

### 2. Verify the Unity version

Use this editor version:

- `6000.4.1f1`

### 3. Open the main scene

Open this scene in Unity:

- `Assets/Scenes/Main.unity`

## Baseline execution

### Run deterministic mode

Deterministic mode verifies the game flow without requiring the local analysis
models.

Use this mode to verify:

- the title screen
- three question cards
- hover and dwell card selection
- question reveal
- macOS TTS narration
- answer collection state changes
- result presentation

Launch Unity in deterministic mode from the repository root:

```bash
MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic open -a "Unity" unity-app
```

If Unity is already open, close it first and relaunch it with the environment
variable.

## Prepare full analysis mode

Full analysis mode uses:

- microphone input
- face frame capture
- Whisper transcription
- face emotion analysis
- voice emotion analysis

This path requires local model assets.

### Local model asset paths

Prepare these locations:

- Face model:
  - `python-engine/models/face/yolo26x_rafdb_best.pt`
- Voice emotion model:
  - `python-engine/models/voice/best_wav2vec2_iemocap/`
- Whisper cache root:
  - `python-engine/models/whisper/`

If internet access is available, Whisper can download `whisper-tiny`
automatically on first use.

The face model and the voice emotion model are not committed to the repository.
Request those assets from the project maintainer.

## Validate the Python bridge

Run the following commands from the repository root:

```bash
conda activate mouth-of-truth
python-engine/scripts/validate_bridge_runtime.sh
```

This validation checks:

- request JSON generation
- Python runner execution
- Whisper transcription
- result JSON generation

The validation succeeds when it prints `Bridge runtime validation succeeded.`

## Verification checklist

The baseline setup is ready when all of the following are true.

- Unity opens from `unity-app/`.
- `Assets/Scenes/Main.unity` opens correctly.
- Unity has no blocking compile errors.
- Card selection works.
- TTS plays.
- A result screen appears.

The full analysis path is ready when all of the following are also true.

- `python -m compileall python-engine/src` passes.
- `validate_bridge_runtime.sh` passes.
- The face model file exists.
- The voice model directory exists.
- The Whisper cache root exists.

## Common mistakes

### Unity was opened from the repository root

Cause:

- The repository root was opened instead of `unity-app/`.

Action:

- Close the incorrectly opened project.
- Reopen `<repo-root>/unity-app` from Unity Hub.

### The game runs but analysis does not

Cause:

- Unity is running in deterministic mode, or the local model assets are missing.

Action:

- Prepare the face and voice model assets for full analysis.
- Relaunch Unity without the deterministic override.

### Whisper transcription does not start

Cause:

- `python-engine/models/whisper/` is missing, or the network is blocked.

Action:

- Verify that `python-engine/models/whisper/` exists.
- Rerun the bridge validation with network access when needed.

## Related documents

- `docs/build-and-distribution-guide-en.md`
- `python-engine/environment.yml`
- `python-engine/requirements.txt`
- `python-engine/models/README.md`
