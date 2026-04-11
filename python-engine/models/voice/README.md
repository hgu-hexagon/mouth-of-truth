# Voice Model Assets

This directory stores the local speech-emotion model used for inference.

## Required directory

- `best_wav2vec2_iemocap/`

## Notes

- This model directory is intentionally not tracked in Git.
- The contained `model.safetensors` file is large enough to be unsuitable for normal Git storage.
- The current project runtime expects this exact directory name unless the resolver code changes.
