# 음성 모델

필수 디렉터리:

```text
best_wav2vec2_iemocap/
```

필수 파일:

```text
config.json
model.safetensors
preprocessor_config.json
```

모델 정보:

```text
데이터셋: IEMOCAP
학습 방식: wav2vec2-base 음성 분류 파인튜닝
레이블: ang, hap, exc, neu, sad, fru
```

사용 코드:

```text
python-engine/src/mouth_of_truth/voice/infer_voice.py
python-engine/src/mouth_of_truth/voice/voice_score_logic.py
```

학습된 wav2vec2 음성 모델 사용:

```bash
export MOUTH_OF_TRUTH_USE_TRAINED_VOICE_MODEL=1
```
