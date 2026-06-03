# Mouth of Truth Build and Distribution Guide

## 1. Purpose

This document explains how to build Mouth of Truth as a distributable macOS or
Windows product package.

After following this guide, a developer should be able to create:

- the macOS release folder and ZIP
- the Windows release folder
- clear user-facing launch instructions

## 2. Release Package Structure

The product is not shipped as a Unity player alone. A release package includes:

- the Unity player
- `python-engine/`
- the packaged Python runtime
- `bridge/`
- art, audio, questions, and tutorial data inside `StreamingAssets`

End users do not need Unity Editor or a Python development environment. Demo
machines that use Leap Motion input do need the Ultraleap tracking runtime
installed and running.

## 3. Build Host Requirements

### macOS

Install:

- macOS
- Unity Hub
- Unity Editor `6000.4.1f1`
- Miniforge or Mambaforge
- `conda-pack`

### Windows

Install:

- Windows
- Unity Hub
- Unity Editor `6000.4.1f1`
- `Windows Build Support (IL2CPP)`
- Visual Studio 2026, or another Visual Studio version compatible with Unity `6000.4.1f1`
- the `Desktop development with C++` workload
- `MSVC x64/x86 build tools`
- `Windows 11 SDK`
- Miniforge or Mambaforge
- `conda-pack`

If Unity Hub does not list `6000.4.1f1`, install it from the Unity Download
Archive. The Windows package should be built from a Windows Unity Editor.

## 4. Pre-Release Validation

Check the following from the repository root.

- The Unity project opens from `unity-app/`.
- The main scene is `unity-app/Assets/Scenes/Main.unity`.
- `python -m compileall python-engine/src` passes.
- `unity-app/Assets/StreamingAssets/audio/questions/` contains narration audio matching the current question IDs.
- `unity-app/Assets/StreamingAssets/art/` contains the product PNG/JPEG assets.

Release builds may resave `Assets/Scenes/Main.unity` and generated background
images. Before distribution, confirm that those changes are the intended
release-scene outputs and commit them together.

## 5. Package the Python Runtime

### macOS

Run from the repository root:

```bash
conda env create -f python-engine/environment.yml
conda activate mouth-of-truth
python-engine/scripts/package_python_runtime.sh
```

If the environment already exists, skip `conda env create`. If the environment
uses a different name, set it explicitly.

```bash
MOUTH_OF_TRUTH_CONDA_ENV=<conda-env-name> \
python-engine/scripts/package_python_runtime.sh
```

After completion, `python-runtime/` must exist at the repository root.

### Windows

Run from Windows PowerShell at the repository root:

```powershell
conda env create -f python-engine/environment.yml
conda activate mouth-of-truth
.\python-engine\scripts\package_python_runtime.ps1
```

If the environment already exists, skip `conda env create`. If the environment
uses a different name, set it explicitly.

```powershell
$env:MOUTH_OF_TRUTH_CONDA_ENV = "<conda-env-name>"
.\python-engine\scripts\package_python_runtime.ps1
```

After completion, `python-runtime-windows/` must exist at the repository root.

## 6. Build the macOS Release

Run from the repository root:

```bash
UNITY_EDITOR_PATH="<unity-editor-executable>" \
./tools/build-macos-release.sh
```

If Unity is installed elsewhere, change `UNITY_EDITOR_PATH`.

Successful output:

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth/python-engine/`
- `dist/macos/MouthOfTruth/python-runtime/`
- `dist/macos/MouthOfTruth/bridge/`
- `dist/macos/MouthOfTruth-macos.zip`

`MouthOfTruth-macos.zip` is the default package to send to macOS users.

## 7. Build the Windows Release

Run from Windows PowerShell at the repository root:

```powershell
.\tools\build-windows-release.ps1
```

If Unity is installed elsewhere, pass the editor path explicitly.

```powershell
.\tools\build-windows-release.ps1 -UnityEditorPath "<unity-editor-executable>"
```

Successful output:

- `dist/windows/MouthOfTruth/MouthOfTruth.exe`
- `dist/windows/MouthOfTruth/Run Mouth of Truth.bat`
- `dist/windows/MouthOfTruth/MouthOfTruth_Data/`
- `dist/windows/MouthOfTruth/python-engine/`
- `dist/windows/MouthOfTruth/python-runtime/`
- `dist/windows/MouthOfTruth/bridge/`

Zip and deliver the full `dist/windows/MouthOfTruth/` folder to Windows users.

## 8. User Launch Instructions

### macOS

Deliver:

- `MouthOfTruth-macos.zip`

Run:

1. Extract the ZIP.
2. Launch `MouthOfTruth/Run Mouth of Truth.command`.
3. If macOS blocks execution, right-click the file in Finder and choose **Open**.
4. Allow camera and microphone permissions.

### Windows

Deliver:

- a ZIP of the Windows-built `MouthOfTruth/` folder

Run:

1. Extract the ZIP.
2. Launch `MouthOfTruth\Run Mouth of Truth.bat`.
3. Allow camera and microphone permissions.

### Leap Motion

For Leap Motion demos, the target machine must have the Ultraleap tracking
runtime installed and running. The device must be visible to the operating
system and hand tracking must be active in the Ultraleap runtime.

## 9. Final Release Checklist

Before distribution, run the normal launcher and verify:

- the title screen opens
- `START GAME` works
- the first-run tutorial appears
- the temple approach sequence plays
- three cards appear
- card hover/dwell selection works
- the selected card flips and question narration plays
- the selected card is absorbed into the Mouth of Truth while the camera zooms at the same time
- the pointer stays settled at bottom center during the hand prompt voice and the prompt fades when insertion movement is detected
- the ritual hand rises from below toward the Mouth of Truth before answer collection starts
- answer collection shows the fixed-base vertically sweeping eye-beam effect
- analysis shows zoom, aura, and shake effects
- the result screen appears
- `TRY AGAIN` returns to the card-selection flow
- the top-left exit button works

## 10. Troubleshooting

### The app opens, but analysis does not run

Check that the release folder contains:

- `python-engine/`
- `python-runtime/`
- `bridge/`

### Leap Motion pointer does not move

Check that:

- the Ultraleap tracking runtime is installed
- the runtime is running
- the Leap Motion device is detected by the OS and Ultraleap Control Panel
- real hand tracking is visible in the runtime

### Windows build fails

Check that:

- Unity has `Windows Build Support (IL2CPP)`
- Visual Studio has the `Desktop development with C++` workload
- `MSVC x64/x86 build tools` and `Windows 11 SDK` are installed
