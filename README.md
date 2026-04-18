# Mouth of Truth

Unity-first game project for the revised Mouth of Truth experience.

## Repository Structure

- `unity-app/`
  The main game shell, presentation flow, input adapters, and release build tools.
- `python-engine/`
  The Python-side analysis bridge, Whisper transcription flow, and verdict policies.
- `assets/`
  Imported and referenced design assets used during production.
- `bridge/`
  Runtime JSON exchange directory used by the Unity-managed analysis flow.

## Current Product Flow

The current repository includes a playable end-to-end flow with:

- title screen with `START GAME` and `EXIT GAME`
- three hidden question cards drawn from a non-repeating JSON pool
- pointer-based hover and dwell card selection
- question reveal, launch-to-mouth animation, and question TTS narration
- hand insertion, answer pause, and answer resume semantics
- microphone answer capture with keyboard transcript fallback
- face frame capture during answer collection
- result presentation for `TRUE`, `FALSE`, and `UNCERTAIN`
- Python bridge analysis with deterministic fallback when the primary analysis path fails
- build automation for macOS and Windows distribution folders

## Input and Analysis Notes

- Real-time pointer input currently comes from the composite hand input adapter:
  - Leap Motion when available
  - mouse pointer fallback when Leap is unavailable
- Answer collection currently uses:
  - maximum answer duration: `8.0s`
  - silence timeout: `1.8s`
  - answer-hold loss grace: `0.35s`
- The Python bridge process timeout is currently `5s` before the Unity layer falls back to deterministic verdict generation.

## Developer Documentation

Committed setup and release guides are available here:

- [developer-setup-checklist-ko.md](docs/developer-setup-checklist-ko.md)
- [developer-setup-checklist-en.md](docs/developer-setup-checklist-en.md)
- [build-and-distribution-guide-ko.md](docs/build-and-distribution-guide-ko.md)
- [build-and-distribution-guide-en.md](docs/build-and-distribution-guide-en.md)
- [manual-validation-without-leap-ko.md](docs/manual-validation-without-leap-ko.md)
- [session-architecture-ko.md](docs/session-architecture-ko.md)
