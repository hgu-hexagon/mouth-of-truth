# 학습 기준 메모

이 폴더는 현재 배포 모델의 원본 학습 코드를 보관하는 위치가 아닙니다. 새 모델을 같은 데이터셋 계열로 다시 학습하거나 교체할 때, 어떤 기준을 남겨야 하는지 정리한 후속 작업 메모입니다.

원본 데이터셋, 전처리 산출물, 체크포인트, 학습 로그는 Git에 넣지 않습니다. 각 데이터셋의 사용 조건을 따르고, 필요한 파일은 별도 저장소나 Release asset으로 관리합니다.

## 범위

현재 저장소가 보장하는 항목:

- 실행 시 필요한 모델 경로와 파일 구조
- 얼굴/음성 레이블 이름과 점수 규칙
- Python 분석 결과와 Unity 브리지 형식
- 모델 묶음 파일의 검사값 검증 흐름

현재 저장소가 그대로 재현한다고 주장하지 않는 항목:

- 기존 체크포인트를 만들 때의 정확한 train/validation/test 분리 기준
- 기존 체크포인트의 실제 하이퍼파라미터와 random seed
- 기존 체크포인트의 학습 로그, confusion matrix, 검증 지표
- 학습 당시 장비와 지연 시간 측정값

## 얼굴 모델 기준

데이터셋:

```text
RAF-DB
https://www.whdeng.cn/RAF/model1.html
```

권장 기준:

- 공식 신청 절차로 받은 RAF-DB 패키지를 사용합니다.
- 가능하면 공식 분리 기준과 aligned image를 우선 사용합니다.
- class 이름은 실행 점수 규칙의 key와 맞춥니다.

```text
happiness
neutral
sadness
surprise
fear
disgust
anger
```

산출물:

```text
python-engine/models/face/yolo26x_rafdb_best.pt
```

학습 코드를 추가한다면 아래 정보를 함께 기록합니다.

| 항목 | 기록 |
| --- | --- |
| 데이터셋 패키지/date | |
| split 기준 | |
| 전처리 | |
| 기반 구조/체크포인트 | |
| seed | |
| epochs/batch size | |
| optimizer/learning rate | |
| 검증 지표 | |
| confusion matrix 경로 | |
| 산출물 검사값 | |

## 음성 모델 기준

데이터셋:

```text
IEMOCAP
https://sail.usc.edu/iemocap/
```

권장 기준:

- 공식 release form과 사용 조건을 따릅니다.
- speaker/session leakage가 생기지 않도록 split 기준을 명시합니다.
- audio는 16 kHz mono waveform 기준으로 전처리합니다.
- `hap`과 `exc`를 분리할지 병합할지 학습 전에 고정합니다.

현재 실행 레이블 순서:

```text
ang
hap
exc
neu
sad
fru
```

산출물:

```text
python-engine/models/voice/best_wav2vec2_iemocap/config.json
python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors
python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json
```

학습 코드를 추가한다면 아래 정보를 함께 기록합니다.

| 항목 | 기록 |
| --- | --- |
| 데이터셋 패키지/date | |
| 화자/세션 분리 기준 | |
| 전처리 | |
| 기반 모델 | |
| seed | |
| epochs/batch size | |
| optimizer/learning rate | |
| 검증 지표 | |
| confusion matrix 경로 | |
| 산출물 검사값 | |

## 평가 기록

새 모델로 교체할 때는 최소한 아래 항목을 릴리스 노트나 별도 평가 산출물에 남깁니다.

| 항목 | 얼굴 | 음성 |
| --- | --- | --- |
| 데이터셋/split | | |
| 주요 지표 | | |
| macro F1 | | |
| confusion matrix | | |
| 평균 지연 시간 | | |
| p95 지연 시간 | | |
| 장비 | | |
| 알려진 실패 사례 | | |

## 교체 체크리스트

1. 모델 파일을 실행 경로에 배치합니다.
2. 레이블 이름과 순서가 점수 규칙과 맞는지 확인합니다.
3. `python-engine/models/README.md`의 검사값을 갱신합니다.
4. `docs/setup-and-release.md`의 모델 복원/검증 정보를 갱신합니다.
5. `unity-app/Assets/Editor/ReleaseRuntimeValidator.cs`의 검사값을 갱신합니다.
6. Python 테스트와 Unity EditMode 테스트를 실행합니다.
7. 실제 설치 장비에서 카메라, 마이크, Ultraleap 지연 시간을 확인합니다.
