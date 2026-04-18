# Mouth of Truth Build and Distribution Guide

## Overview

This document explains how to build and distribute Mouth of Truth as a macOS
and Windows product package.

After completing this guide, you should be able to:

- prepare the distributable Python runtime
- run the macOS release build
- run the Windows release build
- identify the folders that must be delivered to users
- tell users which file they must launch

## Build-host requirements

### macOS build host

Prepare the following:

- macOS
- Unity Editor `6000.4.1f1`
- conda environment `mouth-truth` or `mouth-of-truth`
- `conda-pack`

### Windows build host

Prepare the following:

- Windows
- Unity Editor `6000.4.1f1`
- `Windows Build Support (IL2CPP)`
- Visual Studio 2026
- the `Desktop development with C++` workload
- `MSVC x64/x86 build tools`
- `Windows 11 SDK`
- conda environment `mouth-truth` or `mouth-of-truth`
- `conda-pack`

Important:

- If Unity Hub does not list `6000.4.1f1`, install it from the Unity Download Archive.
- The Windows distribution should be built from a Windows Unity Editor.

## Pre-release checklist

Verify the following first.

- The Unity project opens from `unity-app/`.
- The main scene is `Assets/Scenes/Main.unity`.
- Runtime art assets are present under `unity-app/Assets/StreamingAssets/art/`.
- `python -m compileall python-engine/src` passes.
- On macOS, `python-engine/scripts/validate_bridge_runtime.sh` passes.
- `Mouth Of Truth > Validate Software Flow` passes.
- `Mouth Of Truth > Validate Product Readiness` passes.

## Build the distributable Python runtime

Do not ship the Unity player alone.
The release package must include the Python runtime.

### macOS Python runtime

Run this command from the repository root.

```bash
conda activate mouth-of-truth
python-engine/scripts/package_python_runtime.sh
```

The script performs the following actions.

- packages the `mouth-truth` or `mouth-of-truth` conda environment
- creates `python-runtime/` at the repository root
- expands the distributable Python executable and packages into that directory

If you use a different environment name, set it first.

```bash
MOUTH_OF_TRUTH_CONDA_ENV=<conda-env-name> \
python-engine/scripts/package_python_runtime.sh
```

### Windows Python runtime

Open Windows PowerShell at the repository root and run:

```powershell
conda activate mouth-of-truth
.\python-engine\scripts\package_python_runtime.ps1
```

The script performs the following actions.

- packages the `mouth-truth` or `mouth-of-truth` conda environment
- creates `python-runtime-windows/` at the repository root
- expands the distributable Windows Python executable and packages into that directory

If you use a different environment name, set it first.

```powershell
$env:MOUTH_OF_TRUTH_CONDA_ENV = "<conda-env-name>"
.\python-engine\scripts\package_python_runtime.ps1
```

## Build the macOS release

### 1. Refresh the main scene

Run this Unity menu item.

- `Mouth Of Truth > Build Main Scene`

### 2. Run the macOS release build

Run this Unity menu item.

- `Mouth Of Truth > Build Mac Release`

You can also run the repository script from the repository root.

```bash
./tools/build-macos-release.sh
```

The build creates the following output.

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth/python-engine/`
- `dist/macos/MouthOfTruth/python-runtime/`
- `dist/macos/MouthOfTruth/bridge/`

## Build the Windows release

Run the Windows release from a Windows Unity Editor that has Windows Build
Support installed.

### 1. Refresh the main scene

Run this Unity menu item.

- `Mouth Of Truth > Build Main Scene`

### 2. Run the Windows release build

Run this Unity menu item.

- `Mouth Of Truth > Build Windows Release`

You can also run the repository PowerShell script.

```powershell
.\tools\build-windows-release.ps1
```

If Unity is installed in a different path, pass it explicitly.

```powershell
.\tools\build-windows-release.ps1 -UnityEditorPath "C:\Path\To\Unity.exe"
```

The build creates the following output.

- `dist/windows/MouthOfTruth/MouthOfTruth.exe`
- `dist/windows/MouthOfTruth/Run Mouth of Truth.bat`
- `dist/windows/MouthOfTruth/MouthOfTruth_Data/`
- `dist/windows/MouthOfTruth/python-engine/`
- `dist/windows/MouthOfTruth/python-runtime/`
- `dist/windows/MouthOfTruth/bridge/`

## Folders that must exist after the build

Verify that all of the following exist for macOS.

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth/python-engine/`
- `dist/macos/MouthOfTruth/python-runtime/`
- `dist/macos/MouthOfTruth/bridge/`

Verify that all of the following exist for Windows.

- `dist/windows/MouthOfTruth/MouthOfTruth.exe`
- `dist/windows/MouthOfTruth/Run Mouth of Truth.bat`
- `dist/windows/MouthOfTruth/MouthOfTruth_Data/`
- `dist/windows/MouthOfTruth/python-engine/`
- `dist/windows/MouthOfTruth/python-runtime/`
- `dist/windows/MouthOfTruth/bridge/`

Do not ship the build if any required item is missing.

## Folders to deliver to users

Deliver the entire directory below.

- `dist/macos/MouthOfTruth/`
- `dist/windows/MouthOfTruth/`

Recommended delivery method:

- compress each platform-specific `MouthOfTruth/` directory as a ZIP file

Important:

- Do not ship `MouthOfTruth.app` by itself on macOS.
- Do not ship `MouthOfTruth.exe` by itself on Windows.
- Always ship the full `MouthOfTruth/` directory.

## File the user should launch

After extracting the package, the user should launch:

- macOS:
  - `Run Mouth of Truth.command`
- Windows:
  - `Run Mouth of Truth.bat`

The launcher sets the runtime root correctly and then opens the correct player
for the platform.

As a secondary option, the user can launch:

- `MouthOfTruth.app`
- `MouthOfTruth.exe`

For support and operation, the platform-specific launcher should be the default
instruction.

## First-launch instructions for users

The first launch can prompt for:

- camera permission
- microphone permission

The user must allow both permissions for the analysis features to work.

On macOS, also tell the user:

- if execution is blocked, right-click the file in Finder and select **Open**

## Final release verification

Verify the following flow before distribution.

- The title screen opens.
- `START GAME` works.
- `EXIT GAME` works.
- Card selection works.
- Question TTS plays.
- Answer collection starts after hand insertion.
- Answer pause works when the hand is removed.
- Answer resume works when the hand returns.
- A result screen appears.
- `TRY AGAIN` works.
- `EXIT GAME` works on the result screen.

## Troubleshooting

### The app opens, but analysis does not run

Cause:

- `python-runtime/` or `python-engine/` may be missing.

Action:

- Verify that both directories are present in the release package.

### The Windows build fails before the player is produced

Cause:

- `Windows Build Support (IL2CPP)` may be missing, or
- the Visual Studio C++ build toolchain may be incomplete.

Action:

- Install `Windows Build Support (IL2CPP)` from Unity Hub.
- Verify the `Desktop development with C++` workload in Visual Studio Installer.
