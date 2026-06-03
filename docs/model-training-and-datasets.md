# 모델 학습 데이터셋과 재현 기준

이 문서는 `Mouth of Truth`의 얼굴/음성 분석 모델을 이해하거나 교체하려는 사람이
같은 데이터셋 계열로 후속 작업을 이어갈 수 있도록 정리한 기준입니다.

현재 저장소는 실행과 배포를 위한 inference/runtime 저장소입니다. 원본 데이터셋,
학습용 중간 파일, 학습 로그, 학습 스크립트는 라이선스와 용량 때문에 포함하지
않습니다. 대신 런타임이 기대하는 모델 artifact 위치, label 체계, 점수 규칙,
검증 방법을 고정합니다.

## 요약

| 모델 | 데이터셋/모델 | 용도 | 런타임 artifact |
| --- | --- | --- | --- |
| 얼굴 표정 | RAF-DB | webcam frame에서 얼굴 표정 확률 추론 | `python-engine/models/face/yolo26x_rafdb_best.pt` |
| 음성 감정 | IEMOCAP | answer wav에서 음성 감정 확률 추론 | `python-engine/models/voice/best_wav2vec2_iemocap/` |
| 선택 전사 | `openai/whisper-tiny` | transcript가 비어 있을 때 answer wav 전사 | `python-engine/models/whisper/models--openai--whisper-tiny/` |

필수 모델 bundle은 아래 문서의 checksum과 경로를 따릅니다.

```text
python-engine/models/README.md
```

## 데이터셋 접근

### RAF-DB

공식 페이지:

```text
https://www.whdeng.cn/RAF/model1.html
```

사용 목적:

- 얼굴 표정 classification 모델 학습
- 현재 프로젝트의 얼굴 점수 규칙 입력인 `happiness`, `neutral`, `sadness`,
  `surprise`, `fear`, `disgust`, `anger` 계열 확률 생성

공식 RAF-DB 페이지는 약 3만 장 규모의 real-world facial expression image,
single-label 7-class basic emotion subset, compound emotion subset, landmark,
bounding box, demographic annotations를 설명합니다. 데이터셋은 공개 Git 저장소에
재배포하지 않고, 각 연구자/개발자가 공식 신청 절차와 사용 조건을 따릅니다.

후속 학습자가 확인할 것:

- 신청 승인 후 받은 dataset package의 사용 범위와 재배포 제한
- train/test split과 aligned image 사용 여부
- 7-class basic emotion label 순서
- 학습한 checkpoint의 `model.names`가 런타임 점수 규칙과 맞는지 여부

### IEMOCAP

공식 페이지:

```text
https://sail.usc.edu/iemocap/
```

사용 목적:

- 음성 감정 classification 모델 학습
- 현재 프로젝트의 음성 점수 규칙 입력인 `ang`, `hap`, `exc`, `neu`, `sad`,
  `fru` 확률 생성

USC SAIL의 IEMOCAP 페이지는 약 12시간 분량의 acted multimodal/multispeaker
database를 설명합니다. 이 데이터셋은 audio, video, facial motion capture,
transcription, categorical emotion annotation, dimensional annotation을 포함하는
연구용 데이터셋입니다. 데이터셋 원본과 파생 학습 데이터는 저장소에 넣지 않습니다.

후속 학습자가 확인할 것:

- 공식 release form과 사용 조건
- categorical annotation에서 사용할 label set
- `hap`과 `exc`를 분리할지 병합할지 여부
- speaker/session split 방식
- 16 kHz mono waveform 전처리와 padding/truncation 정책

### Whisper

공식 모델 카드:

```text
https://huggingface.co/openai/whisper-tiny
```

사용 목적:

- `MOUTH_OF_TRUTH_ENABLE_TRANSCRIPTION=1`일 때 answer audio transcript 보조 생성
- 기본 판정에는 transcript가 없어도 얼굴/음성 evidence가 충분하면 판정 가능

Whisper cache는 선택 항목입니다. 오프라인 배포에서 전사를 켤 때만 cache를 함께
포함합니다.

## 런타임 모델 계약

### 얼굴 모델 계약

위치:

```text
python-engine/models/face/yolo26x_rafdb_best.pt
```

로더:

```text
python-engine/src/mouth_of_truth/face/infer_face.py
```

기대 형식:

- Ultralytics `YOLO` classification checkpoint
- `model.predict(face_crop)` 호출 시 `prediction_result.probs`를 반환
- `model.names`가 class index에서 class name으로 매핑
- class name은 아래 점수 규칙에서 사용하는 key와 호환

점수 규칙:

```text
python-engine/src/mouth_of_truth/face/face_score_logic.py
```

현재 점수 규칙이 참조하는 label key:

```text
happiness
neutral
sadness
surprise
fear
disgust
anger
```

교체 checklist:

1. 새 checkpoint를 `python-engine/models/face/yolo26x_rafdb_best.pt`에 둡니다.
2. `python-engine/src/mouth_of_truth/face/infer_face.py`에서 로드되는지 확인합니다.
3. `model.names`가 위 label key와 일치하는지 확인합니다.
4. label 이름이 다르면 `face_score_logic.py`의 mapping도 함께 수정합니다.
5. `tools/package-model-assets.sh`와 `ReleaseRuntimeValidator.cs`의 checksum을 새
   모델 기준으로 갱신합니다.

### 음성 모델 계약

위치:

```text
python-engine/models/voice/best_wav2vec2_iemocap/
```

필수 파일:

```text
config.json
model.safetensors
preprocessor_config.json
```

로더:

```text
python-engine/src/mouth_of_truth/voice/infer_voice.py
```

기대 형식:

- Hugging Face `AutoFeatureExtractor` 호환 directory
- Hugging Face `AutoModelForAudioClassification` 호환 directory
- 16 kHz mono audio 입력
- model output class 수가 `VOICE_LABELS`와 동일

현재 `VOICE_LABELS`:

```text
ang
hap
exc
neu
sad
fru
```

점수 규칙:

```text
python-engine/src/mouth_of_truth/voice/voice_score_logic.py
```

교체 checklist:

1. 새 model directory를 `python-engine/models/voice/best_wav2vec2_iemocap/`에 둡니다.
2. `config.json`, `model.safetensors`, `preprocessor_config.json`을 포함합니다.
3. output label 순서가 `VOICE_LABELS`와 같은지 확인합니다.
4. label 순서나 label 이름이 다르면 `VOICE_LABELS`와 `voice_score_logic.py`를
   함께 수정합니다.
5. `MOUTH_OF_TRUTH_USE_TRAINED_VOICE_MODEL=1`로 trained model path를 실제 실행합니다.
6. `tools/package-model-assets.sh`와 `ReleaseRuntimeValidator.cs`의 checksum을 새
   모델 기준으로 갱신합니다.

## 학습 재현 가이드

이 저장소에는 학습 스크립트가 포함되어 있지 않습니다. 후속 프로젝트에서 학습
코드를 추가할 때는 아래 구조를 권장합니다.

```text
training/
  face/
    prepare_raf_db.py
    train_yolo_raf_db.py
    evaluate_raf_db.py
  voice/
    prepare_iemocap.py
    train_wav2vec2_iemocap.py
    evaluate_iemocap.py
```

단, 원본 dataset, 전처리된 image/audio, 학습 checkpoint, 학습 log는 Git에 넣지
않습니다. 별도 artifact storage, GitHub Release asset, 내부 storage를 사용합니다.

권장 기록 항목:

- dataset version 또는 download/release date
- 신청/사용 조건 확인 날짜
- train/validation/test split
- label mapping
- preprocessing pipeline
- random seed
- model architecture와 base checkpoint
- epoch, batch size, optimizer, learning rate
- 평가 metric과 confusion matrix
- runtime latency
- 모델 artifact checksum

## 현재 판정 정책과 모델 성능 해석

현재 `TRUE` / `FALSE`는 설치형 게임 연출을 위한 rule-based fusion 결과입니다.
학술적 또는 법적 의미의 거짓말 탐지 정확도를 주장하지 않습니다.

최종 fusion 경로:

```text
python-engine/src/mouth_of_truth/fusion/judgment_policy.py
python-engine/src/mouth_of_truth/fusion/multimodal_fusion.py
python-engine/src/mouth_of_truth/fusion/verdict_policy.py
```

현재 정책:

```text
FACE_WEIGHT = 0.80
VOICE_WEIGHT = 0.20
MULTIMODAL_FALSE_PIVOT_SCORE = 33.0
```

운영 환경이 바뀌면 아래를 다시 검증합니다.

- 카메라 높이와 조명에 따른 face recognition count
- 마이크 gain과 주변 소음에 따른 voice segment count
- 얼굴/음성 score distribution
- `UNCERTAIN` 비율
- 참가자가 결과를 게임 연출로 이해하는지 여부

## 모델 bundle을 새로 만들 때

필수 모델 파일을 새 artifact로 교체한 뒤:

```bash
tools/package-model-assets.sh
```

생성물:

```text
dist/model-assets/mouth-of-truth-models-required.tar.gz
dist/model-assets/mouth-of-truth-models-required.tar.gz.sha256
```

Whisper cache까지 배포할 때:

```bash
MOUTH_OF_TRUTH_INCLUDE_WHISPER_CACHE=1 tools/package-model-assets.sh
```

모델을 교체했다면 아래 파일의 checksum도 함께 갱신합니다.

```text
python-engine/models/README.md
docs/setup-and-release.md
unity-app/Assets/Editor/ReleaseRuntimeValidator.cs
tools/package-model-assets.sh
tools/restore-model-assets.sh
tools/restore-model-assets.ps1
```

## 참고 링크

```text
RAF-DB: https://www.whdeng.cn/RAF/model1.html
IEMOCAP: https://sail.usc.edu/iemocap/
Whisper tiny: https://huggingface.co/openai/whisper-tiny
Ultralytics YOLO: https://docs.ultralytics.com/
Hugging Face Transformers: https://huggingface.co/docs/transformers/
Ultraleap setup: https://leap2.ultraleap.com/
```
