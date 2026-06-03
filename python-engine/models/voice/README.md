# Voice Model Assets

Required local directory:

```text
best_wav2vec2_iemocap/
```

Required files inside it:

```text
config.json
model.safetensors
preprocessor_config.json
```

Lineage:

- dataset: IEMOCAP
- model family: wav2vec2-base audio classification
- labels: `ang`, `hap`, `exc`, `neu`, `sad`, `fru`
- runtime loader: `python-engine/src/mouth_of_truth/voice/infer_voice.py`
- score rules: `python-engine/src/mouth_of_truth/voice/voice_score_logic.py`

The trained voice model is optional in the current product default path. Enable
it explicitly with:

```bash
export MOUTH_OF_TRUTH_USE_TRAINED_VOICE_MODEL=1
```

Without that flag, the voice pipeline uses the fast waveform-dynamics path.
