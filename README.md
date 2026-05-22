# Mouth of Truth

Unity-first product project for an interactive Mouth of Truth experience.

The current build is organized around a single product shell: Unity owns the
visible game flow and launches the packaged Python analysis bridge when the
release launcher is used. Python owns the answer analysis logic, model loading,
and verdict generation.

## Repository Structure

- `unity-app/`
  Main Unity project, game flow, presentation layer, input adapters, and release build automation.
- `python-engine/`
  Python analysis bridge, model resolver, face/voice analysis, and verdict policy.
- `assets/`
  Lightweight reference-asset documentation.
- `bridge/`
  Runtime JSON exchange directory used by local validation and release launchers.
- `tools/`
  Build and presentation-capture helper scripts.

## Current Product Flow

The current product flow includes:

- title screen with `START GAME` and top-left exit control
- first-run Leap Motion instruction sequence with a visible device image under the hand guide
- temple approach sequence into the card-selection space
- three hidden question cards drawn from a non-repeating JSON pool
- pointer hover and dwell selection for cards and buttons
- card flip, question reveal, and pre-recorded question narration
- simultaneous card absorption into the Mouth of Truth and camera zoom
- hand prompt voice, hand-in-mouth detection, and bottom-up hand insertion animation
- microphone answer capture with webcam face-frame capture
- answer collection visuals using vertically sweeping fixed-base eye-beam feedback
- analysis presentation with zoom, shake, and aura effects
- result presentation for `TRUE`, `FALSE`, and `UNCERTAIN`
- `TRY AGAIN` flow that returns to the card-selection experience
- presentation-test launcher for automated visual capture

## Input and Analysis Notes

- Pointer input is provided by a composite hand-input adapter:
  - Leap Motion / Ultraleap input when tracking data is available
  - mouse fallback for local development and software-only validation
- Card and button activation use dwell selection with a reacquire guard so newly
  detected hands do not immediately trigger a selection.
- During the hand prompt voice, the pointer remains settled at the bottom center.
  The prompt panel fades only when real hand movement is detected for insertion.
- Answer capture uses Unity microphone input. If microphone initialization fails
  in development, a keyboard transcript adapter can keep the software flow testable.
- The normal Python bridge requires both usable face evidence and usable voice
  evidence before returning `TRUE` or `FALSE`; missing evidence returns `UNCERTAIN`.
- Face evidence is the primary signal and voice evidence is a supporting signal.

## Release Outputs

The macOS release build creates:

- `dist/macos/MouthOfTruth/MouthOfTruth.app`
- `dist/macos/MouthOfTruth/Run Mouth of Truth.command`
- `dist/macos/MouthOfTruth/Run Mouth of Truth Presentation Test.command`
- `dist/macos/MouthOfTruth-macos.zip`

The normal user-facing launcher is `Run Mouth of Truth.command`. The presentation
test launcher runs the same product in capture mode without requiring a spoken
answer.

## Developer Documentation

Committed setup and release guides are available here:

- [docs/developer-setup-checklist-ko.md](docs/developer-setup-checklist-ko.md)
- [docs/developer-setup-checklist-en.md](docs/developer-setup-checklist-en.md)
- [docs/build-and-distribution-guide-ko.md](docs/build-and-distribution-guide-ko.md)
- [docs/build-and-distribution-guide-en.md](docs/build-and-distribution-guide-en.md)
