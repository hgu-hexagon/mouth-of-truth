# Model Assets

This directory contains local inference models used by the current `mouth-of-truth` project.

## Important

- These model files are intentionally **not tracked in Git**.
- The voice model contains large files that can exceed common Git hosting limits.
- Runtime code is expected to resolve models from this local project directory first:
  - `python-engine/models/face/`
  - `python-engine/models/voice/`
  - `python-engine/models/whisper/`

## Required local layout

- `python-engine/models/face/yolo26x_rafdb_best.pt`
- `python-engine/models/voice/best_wav2vec2_iemocap/`
- `python-engine/models/whisper/models--openai--whisper-tiny/`

## Notes

- If you replace the models, keep the same directory structure unless you also update
  the runtime model resolver.
- If needed, you can override the root with `MOUTH_OF_TRUTH_MODELS_ROOT`.
