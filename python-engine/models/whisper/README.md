# Whisper Model Cache

This directory stores the local Hugging Face cache for the Whisper transcription model
used by the current project.

## Expected local layout

- `python-engine/models/whisper/models--openai--whisper-tiny/`

## Notes

- Keep the Hugging Face cache structure intact after copying it into this directory.
- These files are intentionally not tracked in Git because they can be large.
