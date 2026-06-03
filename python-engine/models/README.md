# 모델 자산

이 디렉터리는 모델 배치 구조만 Git에 유지합니다. 실제 모델 바이너리는 Git에 포함하지 않습니다.

데이터셋 신청, 레이블 매핑, 모델 교체 기준, 재학습 시 남길 산출물은 아래 문서를 먼저 확인합니다.

```text
docs/model-training-and-datasets.md
```

## 필수 구조

`mouth-of-truth-models-required.tar.gz` 묶음 파일을 받은 뒤 저장소 루트에서 복원합니다.

```bash
tools/restore-model-assets.sh <path-to>/mouth-of-truth-models-required.tar.gz
```

Windows PowerShell:

```powershell
.\tools\restore-model-assets.ps1 -ModelBundlePath <path-to>\mouth-of-truth-models-required.tar.gz
```

복원 후 구조:

```text
python-engine/models/face/yolo26x_rafdb_best.pt
python-engine/models/voice/best_wav2vec2_iemocap/config.json
python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors
python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
```

## 필수 모델 검증

```text
48e47f019b8214b4c6869af87a3ab8a23fa34a0e891a6d4caf7fd25f7492e35a  python-engine/models/face/yolo26x_rafdb_best.pt
e80a86c0d4e859cd46cc852d4f5864f3de78be8e64f47c1f79b31b687099f5be  python-engine/models/voice/best_wav2vec2_iemocap/config.json
699c55de39fddb538eee49a24afc1008a20bb78918b7a50429b63b59dc62f5c3  python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors
8cdfd65ff4115423185a1512bdae100e2e0cd744f5b322417429944aaafd0827  python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
```

```bash
shasum -a 256 \
  python-engine/models/face/yolo26x_rafdb_best.pt \
  python-engine/models/voice/best_wav2vec2_iemocap/config.json \
  python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors \
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

모델 묶음 파일은 GitHub Release asset, 모델 저장소, 사내 저장소, 또는 다른 외부 저장소로 관리합니다. 일반 GitHub Git push는 100 MiB를 초과하는 단일 파일을 차단합니다.

로컬 모델 파일로 묶음 파일을 만들 때:

```bash
tools/package-model-assets.sh
```

생성 위치:

```text
dist/model-assets/mouth-of-truth-models-required.tar.gz
dist/model-assets/mouth-of-truth-models-required.tar.gz.sha256
```

## 모델 교체

배포본은 학습된 모델 산출물을 사용합니다. 다른 모델을 사용할 때는 같은 경로와 출력 포맷을 맞추고, 얼굴/음성 점수 규칙이 기대하는 레이블 체계를 유지합니다.
