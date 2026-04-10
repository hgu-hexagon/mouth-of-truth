# New Mouth of Truth

Unity-first game prototype for the revised Mouth of Truth assignment.

## Structure

- `unity-app/`
  The new game shell and presentation layer.
- `python-engine/`
  Future analysis adapters and reusable Python-side contracts.
- `assets/`
  Imported or referenced design assets.
- `bridge/`
  Runtime JSON exchange directory kept compatible with managed-engine flows.

## Current Focus

The first implementation stage establishes:

- question pool loading from JSON
- non-repeating three-card round generation
- card dwell selection rules
- hand insertion and pause/resume state semantics
- verdict model for `TRUE`, `FALSE`, and `UNCERTAIN`

Detailed Korean design and contract notes are intentionally left uncommitted.
