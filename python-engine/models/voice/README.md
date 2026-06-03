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
데이터셋 신청: https://sail.usc.edu/iemocap/
학습 방식: wav2vec2-base 음성 분류 파인튜닝
레이블: ang, hap, exc, neu, sad, fru
```

SHA-256:

```text
config.json: e80a86c0d4e859cd46cc852d4f5864f3de78be8e64f47c1f79b31b687099f5be
model.safetensors: 699c55de39fddb538eee49a24afc1008a20bb78918b7a50429b63b59dc62f5c3
preprocessor_config.json: 8cdfd65ff4115423185a1512bdae100e2e0cd744f5b322417429944aaafd0827
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

동일 재학습에는 IEMOCAP 접근 권한, 전처리 코드, split, 학습 명령,
hyperparameter, seed, checkpoint 선택 기준이 필요합니다.
