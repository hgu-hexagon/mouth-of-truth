# Mouth of Truth Build and Distribution Guide

## Overview

This document explains how to build and distribute Mouth of Truth as a macOS
product package.

After completing this guide, you should be able to:

- prepare the distributable Python runtime
- run the macOS release build
- identify the files that must be delivered to users
- tell users which file they must launch

## Pre-release checklist

Verify the following first.

- The Unity project opens from `unity-app/`.
- The main scene is `Assets/Scenes/Main.unity`.
- Final PNG assets are applied under `unity-app/Assets/StreamingAssets/art/`.
- Leap Motion integration and hardware validation are complete.
- The local model assets for Python analysis are ready.
- `python-engine/scripts/validate_bridge_runtime.sh` passes.

## Build the distributable Python runtime

Do not ship the Unity `.app` bundle alone.
The release package must include the Python runtime.

Run this command from the repository root.

```bash
conda activate mouth-of-truth
python-engine/scripts/package_python_runtime.sh
```

The script performs the following actions.

- Packages the `mouth-of-truth` conda environment
- Creates `python-runtime/` at the repository root
- Expands the distributable Python executable and packages into that directory

If you use a different environment name, set it first.

```bash
MOUTH_OF_TRUTH_CONDA_ENV=<conda-env-name> \
python-engine/scripts/package_python_runtime.sh
```

## Build the macOS release

### 1. Refresh the main scene

Run this Unity menu item.

- `Mouth Of Truth > Build Main Scene`

This step regenerates `Main.unity` from the current environment assets and
anchor layout.

### 2. Run the macOS release build

Run this Unity menu item.

- `Mouth Of Truth > Build Mac Release`

The build creates the following output.

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth/python-engine/`
- `dist/macos/MouthOfTruth/python-runtime/`
- `dist/macos/MouthOfTruth/bridge/`

## Files that must exist after the build

Verify that all of the following exist.

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth/python-engine/`
- `dist/macos/MouthOfTruth/python-runtime/`
- `dist/macos/MouthOfTruth/bridge/`

Do not ship the build if any of these items are missing.

## Files to deliver to users

Deliver the entire directory below.

- `dist/macos/MouthOfTruth/`

Recommended delivery method:

- compress `MouthOfTruth/` as a ZIP file

Important:

- Do not ship `MouthOfTruth.app` by itself.
- Always ship the full `MouthOfTruth/` directory.

## File the user should launch

After extracting the package, the user should launch:

- `Run Mouth of Truth.command`

The launcher sets the runtime root correctly and then opens
`MouthOfTruth.app`.

As a secondary option, the user can launch:

- `MouthOfTruth.app`

For support and operation, the `.command` launcher should be the default
instruction.

## First-launch instructions for users

The first launch can prompt for:

- camera permission
- microphone permission

The user must allow both permissions for the analysis features to work.

For internal demo builds on macOS, also tell the user:

- if macOS blocks execution, right-click the file in Finder and select **Open**

## Final release verification

Verify the following flow before distribution.

- The title screen opens.
- `START GAME` works.
- Card selection works.
- Question TTS plays.
- Answer collection starts after hand insertion.
- Answer pause works when the hand is removed.
- Answer resume works when the hand returns.
- A result screen appears.
- `TRY AGAIN` and `BACK TO TITLE` work.

## Troubleshooting

### The app opens but analysis does not run

Cause:

- `python-runtime/` or `python-engine/` may be missing.

Action:

- Verify that both directories are present in the release package.

### The user launched only the `.app` bundle

Cause:

- The app bundle was copied without the product root directory.

Action:

- Re-deliver the full `MouthOfTruth/` directory.
- Ask the user to launch `Run Mouth of Truth.command`.

### Packaging `python-runtime/` fails

Cause:

- `conda-pack` is missing, or the conda environment name is different.

Action:

- Recreate the environment with `python-engine/environment.yml`.
- Set `MOUTH_OF_TRUTH_CONDA_ENV` when needed and rerun the packaging script.

## Related documents

- `docs/developer-setup-checklist-en.md`
- `python-engine/environment.yml`
- `python-engine/requirements.txt`
- `python-engine/scripts/package_python_runtime.sh`
