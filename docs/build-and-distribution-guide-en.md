# Mouth of Truth Build and Distribution Guide

## Overview

This document explains how to build and distribute the Mouth of Truth project
as a final product package.

This document assumes the following conditions.

- Final PNG assets are already applied to the project.
- Leap Motion input is already integrated.
- The Python analysis runtime and local model assets are ready.

After completing this guide, you should be able to:

- build the macOS distribution package
- gather all required runtime files
- decide which files must be shipped to users
- tell users which file they must launch

## Pre-build checklist

Verify the following first.

- The Unity project opens from `unity-app/`.
- The main scene is `Assets/Scenes/Main.unity`.
- The Python environment is ready.
- The distributable Python runtime folder is ready.
- The face model, voice model, and Whisper cache exist under `python-engine/models/`.
- Final PNG assets are applied under `unity-app/Assets/StreamingAssets/art/`.
- Leap Motion packages and scene bindings are complete.

## Distribution package layout

The release unit is **not a single app file**.

Use this directory layout as the release baseline.

- `MouthOfTruth/`
  - `MouthOfTruth.app`
  - `Run Mouth of Truth.command`
  - `python-engine/`
  - `python-runtime/`
  - `bridge/`

Each item has a specific role.

- `MouthOfTruth.app`
  - the Unity player
- `Run Mouth of Truth.command`
  - the macOS launcher that starts the app from the correct runtime root
- `python-engine/`
  - the analysis bridge scripts and Python modules
- `python-runtime/`
  - the distributable Python executable and package runtime
- `bridge/`
  - the runtime JSON exchange directory between Unity and Python

Important:

- Do not ship the `.app` bundle alone.
- Ship the entire `MouthOfTruth/` directory.

## Build the macOS release

### 0. Prepare the distributable Python runtime

Prepare one of the following:

- place a `python-runtime/` folder at the repository root
- or set `MOUTH_OF_TRUTH_PYTHON_RUNTIME_ROOT` to the distributable Python runtime folder

The distributable Python runtime must include:

- `bin/python` or an equivalent Python executable
- the Python packages required by this project

### 1. Refresh the main scene

Run this Unity menu item first.

- `Mouth Of Truth > Build Main Scene`

This step updates the main scene using the current environment assets and anchor
layout.

### 2. Build the macOS release package

Run this Unity menu item.

- `Mouth Of Truth > Build Mac Release`

This build performs the following actions.

- Builds `Main.unity` into a macOS app
- Creates the output under `dist/macos/MouthOfTruth/`
- Copies `python-engine/`
- Creates the `bridge/` directory
- Generates the `Run Mouth of Truth.command` launcher
- Copies `python-runtime/` when the runtime folder is available

Note:

- The first macOS release build can take a long time while Unity prepares the
  URP shader cache.
- If the build is slow but the console does not show an error, let it continue
  until it finishes.

## Build output location

When the build succeeds, the output is created here.

- `dist/macos/MouthOfTruth/`

The primary artifacts are:

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`

## Pre-release verification

Verify the following before distribution.

- `MouthOfTruth.app` exists
- `Run Mouth of Truth.command` exists
- `python-engine/` exists
- `python-runtime/` exists
- `bridge/` exists

Then verify the baseline runtime flow.

- The title screen opens.
- Card selection works.
- Question TTS plays.
- Answer collection starts after hand insertion.
- The result screen appears.

## Files to deliver to users

Deliver the entire directory below to users.

- `dist/macos/MouthOfTruth/`

Recommended delivery methods:

- compress the folder as a ZIP file
- ship it as the internal release package before installer wrapping

## File the user should launch

After extracting the package, the user should launch:

- `Run Mouth of Truth.command`

This launcher sets the correct runtime root and then opens `MouthOfTruth.app`.

As a secondary option, the user can launch:

- `MouthOfTruth.app`

For operation and support, the `.command` launcher is the recommended entry
point.

## Troubleshooting

### The app opens but analysis does not run

Cause:

- `python-runtime/` or `python-engine/` may be missing.

Action:

- Verify that both directories are present in the distribution package.

### Only the `.app` bundle was shipped

Cause:

- Shipping only the `.app` bundle omits the Python runtime and the bridge
  directory.

Action:

- Repackage and ship the full `MouthOfTruth/` directory.

### The user cannot launch the app on macOS

Cause:

- macOS security settings or signing policy may block execution.

Action:

- Add code signing and notarization for the chosen release channel.
- For internal demos, provide the required security exception guidance.

## Related documents

- `docs/developer-setup-checklist-en.md`
- `python-engine/environment.yml`
- `python-engine/requirements.txt`
- `unity-app/Assets/Scenes/Main.unity`
