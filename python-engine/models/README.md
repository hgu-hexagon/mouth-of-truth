# 모델 자산

이 디렉터리는 모델 배치 구조만 Git에 유지합니다. 실제 모델 바이너리는 Git에
포함하지 않습니다.

## 필수 구조

```text
python-engine/models/face/yolo26x_rafdb_best.pt
python-engine/models/voice/best_wav2vec2_iemocap/config.json
python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors
python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
```

## 선택 구조

```text
python-engine/models/whisper/models--openai--whisper-tiny/
```

Whisper 캐시는 전사를 사용할 때만 필요합니다.

```bash
export MOUTH_OF_TRUTH_ENABLE_TRANSCRIPTION=1
```

## 다른 모델 루트 사용

```bash
export MOUTH_OF_TRUTH_MODELS_ROOT="/path/to/model-root"
```

위 경로 아래에 같은 구조를 둡니다.

```text
face/yolo26x_rafdb_best.pt
voice/best_wav2vec2_iemocap/
whisper/models--openai--whisper-tiny/
```

## 대용량 파일 관리

모델 파일은 Git LFS, GitHub Release asset, 별도 모델 저장소, 또는 사내 저장소로
관리합니다. 일반 GitHub Git push는 100 MiB를 초과하는 단일 파일을 차단합니다.
