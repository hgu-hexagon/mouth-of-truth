# Model Assets

This directory keeps the required model layout in Git, but not the large model
binaries themselves.

## Why The Models Are Not Tracked

The current local model set includes files that are too large for a normal
GitHub source repository. GitHub blocks ordinary Git pushes containing files
larger than 100 MiB, and these model/cache files also make the repository
unnecessarily heavy for source review.

Keep the actual binaries in one of these places instead:

- internal artifact storage
- GitHub Release assets
- Git LFS, if the project chooses to adopt it
- a manually shared model bundle

## Required Layout

```text
python-engine/models/face/yolo26x_rafdb_best.pt
python-engine/models/voice/best_wav2vec2_iemocap/config.json
python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors
python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
```

Optional Whisper cache:

```text
python-engine/models/whisper/models--openai--whisper-tiny/
```

## Override

The runtime searches this directory by default. To use another model root:

```bash
export MOUTH_OF_TRUTH_MODELS_ROOT="/path/to/model-root"
```

The configured root must contain the same `face/`, `voice/`, and optional
`whisper/` structure.
