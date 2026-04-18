# Mouth of Truth Developer Setup Guide

## Overview

This document explains how to set up the current Mouth of Truth project on a
fresh machine after cloning the GitHub repository.

After completing this guide, you should be able to:

- clone the repository
- create the Python environment
- open the Unity project from the correct path
- run deterministic mode and Python bridge mode
- prepare the required local model assets
- run the core software-flow validations

## Prerequisites

Install the following tools first.

- Git
- Unity Hub
- Unity Editor `6000.4.1f1`
- Miniforge or Mambaforge

If you will build Windows releases, install the following on Windows as well.

- `Windows Build Support (IL2CPP)`
- Visual Studio 2026
- the `Desktop development with C++` workload
- `MSVC x64/x86 build tools`
- `Windows 11 SDK`

Important:

- If Unity Hub does not list `6000.4.1f1` directly, install it from the Unity Download Archive.
- The current project baseline is `6000.4.1f1`.

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

## Prepare the local model assets

Full analysis mode uses the following local model assets.

- Face model:
  - `python-engine/models/face/yolo26x_rafdb_best.pt`
- Voice emotion model:
  - `python-engine/models/voice/best_wav2vec2_iemocap/`
- Whisper cache root:
  - `python-engine/models/whisper/`

Important:

- The face and voice models are not committed to the repository.
- Request those assets from the project maintainer.
- Whisper can download `whisper-tiny` on first use if the machine has internet access.

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

## Runtime modes

The analysis client selection follows this order.

1. `MOUTH_OF_TRUTH_ANALYSIS_MODE=python` forces Python bridge mode
2. `MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic` forces deterministic mode
3. Without an override:
   - Python bridge mode is used when the launcher script and module root exist
   - deterministic mode is used otherwise

## Run deterministic mode

Deterministic mode verifies the product flow without requiring the local
analysis models.

Use this mode to verify:

- the title screen
- three question cards
- hover and dwell selection
- question reveal
- question TTS narration
- hand insertion, pause, and resume
- result presentation

On macOS, launch Unity like this from the repository root:

```bash
MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic open -a "Unity" unity-app
```

On Windows, launch Unity from PowerShell:

```powershell
$env:MOUTH_OF_TRUTH_ANALYSIS_MODE = "deterministic"
$unityEditorPath = "<path-to-unity-editor>"
Start-Process $unityEditorPath -ArgumentList "-projectPath `"<repo-root>\unity-app`""
```

## Run Python bridge mode

Python bridge mode uses Whisper transcription together with the face and voice
analysis path.

macOS example:

```bash
conda activate mouth-of-truth
open -a "Unity" unity-app
```

Windows example:

```powershell
conda activate mouth-of-truth
$unityEditorPath = "<path-to-unity-editor>"
Start-Process $unityEditorPath -ArgumentList "-projectPath `"<repo-root>\unity-app`""
```

## Validate the Python bridge

### macOS

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
- voice-only verdict fallback generation

### Windows

Do not use the macOS `say`-based bridge validation script directly on Windows.
Instead, verify the following first:

- `python -m compileall python-engine/src`
- `Mouth Of Truth > Validate Software Flow`
- `Mouth Of Truth > Validate Product Readiness`

## Verification checklist

The baseline setup is ready when all of the following are true.

- Unity opens from `unity-app/`.
- `Assets/Scenes/Main.unity` opens correctly.
- Unity has no blocking compile errors.
- The title screen shows `START GAME` and `EXIT GAME`.
- Card selection works.
- TTS plays.
- The answer stage starts after mouth insertion.
- A result screen appears.

The full analysis path is ready when all of the following are also true.

- `python -m compileall python-engine/src` passes.
- The face model file exists.
- The voice model directory exists.
- The Whisper cache root exists.
- `validate_bridge_runtime.sh` passes on macOS.

## Common mistakes

### Unity was opened from the repository root

Cause:

- The repository root was opened instead of `unity-app/`.

Action:

- Close the incorrectly opened project.
- Reopen `<repo-root>/unity-app` from Unity Hub.

### Unity Hub does not show `6000.4.1f1`

Cause:

- Hub can prioritize newer patch releases in the install list.

Action:

- Open the Unity Download Archive and install `6000.4.1f1` directly.

### The game runs, but analysis keeps falling back to deterministic mode

Cause:

- Python bridge mode is not available, or the environment override forces deterministic mode.

Action:

- Check `MOUTH_OF_TRUTH_ANALYSIS_MODE`.
- Verify that `python-engine/scripts/` and `python-engine/src/` are present.

### No microphone is available, so the game drops to keyboard transcript fallback

Cause:

- No microphone device is available, or microphone startup failed.

Action:

- Check OS microphone permissions.
- Verify that a working microphone device is present.
- The keyboard transcript fallback is still a valid flow for software validation.

## Related documents

- [build-and-distribution-guide-en.md](build-and-distribution-guide-en.md)
- [manual-validation-without-leap-ko.md](manual-validation-without-leap-ko.md)
- [session-architecture-ko.md](session-architecture-ko.md)
- [python-engine/models/README.md](../python-engine/models/README.md)
