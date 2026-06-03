# Mouth of Truth Developer Setup Guide

## 1. Purpose

This document explains how a new developer can clone the `mouth-of-truth`
repository, run the project, and verify the baseline product flow.

After following this guide, a developer should be able to:

- clone the repository
- create the Python development environment
- open the Unity project from the correct path
- place the local model assets
- build the macOS release package

## 2. Prerequisites

Install:

- Git
- Unity Hub
- Unity Editor `6000.4.1f1`
- Miniforge or Mambaforge
- `conda-pack`

For Windows release builds, install these on Windows as well:

- `Windows Build Support (IL2CPP)`
- Visual Studio 2026, or another Visual Studio version compatible with Unity `6000.4.1f1`
- the `Desktop development with C++` workload
- `MSVC x64/x86 build tools`
- `Windows 11 SDK`

For real Leap Motion use, install:

- Ultraleap tracking runtime
- a Leap Motion or Ultraleap-compatible hand-tracking device

## 3. Clone the Repository

Run:

```bash
git clone https://github.com/hgu-hexagon/mouth-of-truth.git
cd mouth-of-truth
```

This guide refers to that directory as `<repo-root>`.

## 4. Project Structure

The two main roots are:

- Unity project:
  - `<repo-root>/unity-app`
- Python engine:
  - `<repo-root>/python-engine`

Always open `<repo-root>/unity-app` in Unity Hub. Do not open the repository
root as a Unity project.

## 5. Create the Python Environment

Run from the repository root:

```bash
conda env create -f python-engine/environment.yml
conda activate mouth-of-truth
python --version
python -m compileall python-engine/src
```

If `compileall` passes, the Python source tree is readable and compilable in the
current environment.

## 6. Prepare Local Model Assets

The full analysis path uses local model assets. These files are not committed to
Git, so request them from the project maintainer and place them at:

- face expression model:
  - `python-engine/models/face/yolo26x_rafdb_best.pt`
- voice emotion model:
  - `python-engine/models/voice/best_wav2vec2_iemocap/`
- Whisper cache:
  - `python-engine/models/whisper/`

The default product path prioritizes fast face/voice evidence extraction.
Whisper transcription and the heavier wav2vec2 inference path remain available
for optional local diagnostics.

## 7. Open the Unity Project

1. Open Unity Hub.
2. Add `<repo-root>/unity-app`.
3. Open it with Unity Editor `6000.4.1f1`.
4. Confirm that `Assets/Scenes/Main.unity` opens.

## 8. Runtime Modes

The analysis client is selected in this order:

1. `MOUTH_OF_TRUTH_ANALYSIS_MODE=python` forces Python bridge mode.
2. `MOUTH_OF_TRUTH_ANALYSIS_MODE=deterministic` forces deterministic mode.
3. Without an override, Python bridge mode is used when the launcher script and module root are available.
4. If Python bridge mode is unavailable, deterministic fallback is used.

The normal Python bridge returns `TRUE` or `FALSE` only when both usable face
evidence and usable voice evidence exist. Missing evidence returns `UNCERTAIN`.

## 9. Manual Product Flow Check

In Unity Play Mode or a release build, verify:

1. Select `START GAME` on the title screen.
2. Confirm the first-run tutorial.
3. Confirm the temple approach sequence.
4. Confirm three question cards.
5. Select a card with hover/dwell.
6. Confirm the card flip and pre-recorded question narration.
7. Confirm that the selected card is absorbed into the Mouth of Truth while the camera zooms at the same time.
8. Confirm that the pointer stays settled at bottom center during the hand prompt voice and the prompt fades only when insertion movement is detected.
9. Confirm that the ritual hand rises from below toward the Mouth of Truth.
10. Confirm answer collection with fixed-base vertically sweeping eye-beam visuals.
11. Confirm analysis zoom, aura, and shake visuals.
12. Confirm `TRUE`, `FALSE`, and `UNCERTAIN` result presentation.
13. Confirm `TRY AGAIN` and the top-left exit button.

## 10. macOS Release Build

Package the Python runtime first:

```bash
conda activate mouth-of-truth
python-engine/scripts/package_python_runtime.sh
```

Then run:

```bash
UNITY_EDITOR_PATH="<unity-editor-executable>" \
./tools/build-macos-release.sh
```

Successful output includes:

- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth-macos.zip`

Use `Run Mouth of Truth.command` for demos and user runs.

## 11. Common Mistakes

### Unity was opened from the repository root

Close the incorrect project and reopen `<repo-root>/unity-app`.

### Unity Hub does not show `6000.4.1f1`

Install `6000.4.1f1` directly from the Unity Download Archive.

### The Leap Motion pointer does not move

Check that:

- the Ultraleap tracking runtime is installed
- the runtime is running
- the device and hand tracking are visible in the Control Panel

### The result keeps returning `UNCERTAIN`

The current product policy requires both face and voice evidence before
returning `TRUE` or `FALSE`. Check camera permission, microphone permission,
face placement, and audible speech.

## 12. Related Documents

- [build-and-distribution-guide-en.md](build-and-distribution-guide-en.md)
- [developer-setup-checklist-ko.md](developer-setup-checklist-ko.md)
- [build-and-distribution-guide-ko.md](build-and-distribution-guide-ko.md)
- [python-engine/models/README.md](../python-engine/models/README.md)
