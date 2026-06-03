# Whisper Model Cache

Optional local cache:

```text
models--openai--whisper-tiny/
```

This is a Hugging Face cache for `openai/whisper-tiny`. It is not a model trained
by this project.

The cache is only needed when answer transcription is enabled:

```bash
export MOUTH_OF_TRUTH_ENABLE_TRANSCRIPTION=1
```

Keep the Hugging Face cache directory structure intact when copying it here.
