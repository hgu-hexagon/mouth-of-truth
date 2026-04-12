# Mouth of Truth

Unity-first game project for the revised Mouth of Truth experience.

## Structure

- `unity-app/`
  The game shell and presentation layer.
- `python-engine/`
  Future analysis adapters and reusable Python-side contracts.
- `assets/`
  Imported or referenced design assets.
- `bridge/`
  Runtime JSON exchange directory kept compatible with managed-engine flows.

## Current Runtime

The current repository already includes a playable placeholder flow with:

- title screen and start button
- three hidden question cards drawn from a non-repeating JSON pool
- hover + dwell card selection
- question reveal and macOS TTS narration fallback
- hand insertion / pause / resume semantics through a replaceable input adapter
- answer timeout and silence-based completion rules
- verdict model for `TRUE`, `FALSE`, and `UNCERTAIN`
- placeholder art paths that can be swapped without changing code
- Python bridge contracts ready for real analysis integration

Detailed Korean design and contract notes are intentionally left uncommitted.

## Developer setup

Committed setup guides are available here:

- `docs/developer-setup-checklist-ko.md`
- `docs/developer-setup-checklist-en.md`
- `docs/build-and-distribution-guide-ko.md`
- `docs/build-and-distribution-guide-en.md`
